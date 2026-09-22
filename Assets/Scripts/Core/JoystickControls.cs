using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using ETouch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TrackedPoseDriver = UnityEngine.InputSystem.XR.TrackedPoseDriver;

namespace MuseoAutista
{
    /// <summary>
    /// Controles de la versión Joystick (pantalla completa, sin visor):
    /// joystick a la izquierda para caminar, arrastrar para mirar, botón de ACCIÓN
    /// (usa lo que está bajo el punto central), botón CORRER y panel de ajustes
    /// (velocidad y sensibilidad, guardados en el celular).
    /// Editor: WASD caminar, Shift correr, clic derecho + ratón mirar, E/Espacio acción.
    /// </summary>
    public class JoystickControls : MonoBehaviour
    {
        public static bool TapThisFrame { get; private set; }

        const string SpeedKey = "joy_speed", SensKey = "joy_sens";
        float speed = 4.5f;
        float lookSensitivity = 0.14f;
        const float RunMultiplier = 1.9f;

        Transform rig, head;
        CharacterController cc;
        RectTransform baseRt, knobRt, actionRt, runRt, gearRt, panelRt;
        Image actionImg, runImg;
        Text actionText, speedText, sensText;
        CanvasGroup hint;
        float radius, hintTime;
        int moveFinger = -1, lookFinger = -1;
        readonly HashSet<int> buttonFingers = new HashSet<int>();
        Vector2 moveInput;
        bool lookMoved, running;
        float yaw, pitch;
        readonly List<(RectTransform rt, Action onTap)> buttons = new List<(RectTransform, Action)>();

        void Start()
        {
            EnhancedTouchSupport.Enable();
            speed = PlayerPrefs.GetFloat(SpeedKey, speed);
            lookSensitivity = PlayerPrefs.GetFloat(SensKey, lookSensitivity);

            var pr = PlayerRig.Instance;
            rig = pr.transform;
            head = pr.head;
            // Sin visor, el TrackedPoseDriver de Cardboard fijaría la cámara mirando al frente.
            foreach (var tpd in head.GetComponents<TrackedPoseDriver>()) tpd.enabled = false;
            Vector3 e = head.localEulerAngles;
            yaw = e.y;
            pitch = e.x > 180f ? e.x - 360f : e.x;

            cc = rig.gameObject.GetComponent<CharacterController>();
            if (cc == null) cc = rig.gameObject.AddComponent<CharacterController>();
            cc.height = 1.7f;
            cc.radius = 0.3f;
            cc.center = new Vector3(0f, 0.9f, 0f);
            cc.stepOffset = 0.3f;
            cc.skinWidth = 0.03f;

            BuildUI();
        }

        // ------------------------------------------------------------------ interfaz

        void BuildUI()
        {
            var go = new GameObject("ControlesJoystick", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 1f;
            var root = go.transform;
            var circle = CircleSprite();

            baseRt = MakeImage(root, "Base", circle, new Color(1f, 1f, 1f, 0.22f), 300f, new Vector2(0f, 0f), new Vector2(260f, 260f));
            knobRt = MakeImage(baseRt, "Palanca", circle, new Color(1f, 1f, 1f, 0.55f), 130f, new Vector2(0.5f, 0.5f), Vector2.zero);

            actionRt = MakeImage(root, "BotonAccion", circle, Color.white, 250f, new Vector2(1f, 0f), new Vector2(-240f, 240f));
            actionImg = actionRt.GetComponent<Image>();
            actionText = MakeText(actionRt, "ACCIÓN", 44);
            buttons.Add((actionRt, DoAction));

            runRt = MakeImage(root, "BotonCorrer", circle, Color.white, 170f, new Vector2(1f, 0f), new Vector2(-470f, 150f));
            runImg = runRt.GetComponent<Image>();
            MakeText(runRt, "CORRER", 34);
            buttons.Add((runRt, () => running = !running));

            gearRt = MakeImage(root, "BotonAjustes", null, new Color(0f, 0f, 0f, 0.45f), 0f, new Vector2(1f, 1f), new Vector2(-130f, -70f));
            gearRt.sizeDelta = new Vector2(220f, 90f);
            MakeText(gearRt, "AJUSTES", 34);
            buttons.Add((gearRt, () => panelRt.gameObject.SetActive(!panelRt.gameObject.activeSelf)));

            BuildSettingsPanel(root);

            var hintRt = MakeImage(root, "Ayuda", null, new Color(0f, 0f, 0f, 0.45f), 0f, new Vector2(0.5f, 1f), new Vector2(0f, -70f));
            hintRt.sizeDelta = new Vector2(1700f, 90f);
            MakeText(hintRt, "Joystick: caminar · Arrastra: mirar · ACCIÓN: usar lo del centro · AJUSTES: velocidad", 38);
            hint = hintRt.gameObject.AddComponent<CanvasGroup>();
            hintTime = Time.time;
        }

        void BuildSettingsPanel(Transform root)
        {
            panelRt = MakeImage(root, "PanelAjustes", null, new Color(0.08f, 0.09f, 0.12f, 0.9f), 0f, new Vector2(1f, 1f), new Vector2(-400f, -330f));
            panelRt.sizeDelta = new Vector2(600f, 360f);
            var title = MakeText(panelRt, "AJUSTES", 42);
            ((RectTransform)title.transform).anchoredPosition = new Vector2(0f, 130f);
            ((RectTransform)title.transform).sizeDelta = new Vector2(-40f, -290f);

            speedText = Row(panelRt, 40f, "Velocidad", () => ChangeSpeed(-0.5f), () => ChangeSpeed(0.5f));
            sensText = Row(panelRt, -80f, "Sensibilidad", () => ChangeSens(-0.02f), () => ChangeSens(0.02f));
            RefreshSettings();
            panelRt.gameObject.SetActive(false);
        }

        Text Row(RectTransform panel, float y, string label, Action minus, Action plus)
        {
            var text = MakeText(panel, "", 36);
            var trt = (RectTransform)text.transform;
            trt.anchorMin = trt.anchorMax = new Vector2(0.5f, 0.5f);
            trt.sizeDelta = new Vector2(300f, 80f);
            trt.anchoredPosition = new Vector2(0f, y);
            text.name = label;
            var m = MakeImage(panel, "Menos", null, new Color(0.3f, 0.75f, 0.85f, 0.8f), 90f, new Vector2(0.5f, 0.5f), new Vector2(-220f, y));
            MakeText(m, "-", 64);
            var p = MakeImage(panel, "Mas", null, new Color(0.3f, 0.75f, 0.85f, 0.8f), 90f, new Vector2(0.5f, 0.5f), new Vector2(220f, y));
            MakeText(p, "+", 60);
            buttons.Add((m, minus));
            buttons.Add((p, plus));
            return text;
        }

        void ChangeSpeed(float d)
        {
            speed = Mathf.Clamp(speed + d, 1f, 10f);
            PlayerPrefs.SetFloat(SpeedKey, speed);
            RefreshSettings();
        }

        void ChangeSens(float d)
        {
            lookSensitivity = Mathf.Clamp(lookSensitivity + d, 0.04f, 0.4f);
            PlayerPrefs.SetFloat(SensKey, lookSensitivity);
            RefreshSettings();
        }

        void RefreshSettings()
        {
            speedText.text = "Velocidad\n" + speed.ToString("0.0");
            sensText.text = "Sensibilidad\n" + Mathf.RoundToInt(lookSensitivity * 100f);
        }

        static RectTransform MakeImage(Transform parent, string name, Sprite s, Color c, float size, Vector2 anchor, Vector2 pos)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.sprite = s;
            img.color = c;
            img.raycastTarget = false;
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.sizeDelta = new Vector2(size, size);
            rt.anchoredPosition = pos;
            return rt;
        }

        static Text MakeText(Transform parent, string text, int size)
        {
            var go = new GameObject("Texto", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            var t = go.AddComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = size;
            t.fontStyle = FontStyle.Bold;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = Color.white;
            t.raycastTarget = false;
            t.text = text;
            return t;
        }

        static Sprite CircleSprite()
        {
            const int n = 128;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(n / 2f, n / 2f)) / (n / 2f);
                    float a = Mathf.Clamp01((1f - d) * n * 0.5f);
                    float ring = d > 0.86f ? 1f : 0.75f;
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * ring));
                }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f));
        }

        // ------------------------------------------------------------------ entrada

        void Update()
        {
            TapThisFrame = false;
            if (rig == null) return;

            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (SceneManager.GetActiveScene().name == SceneNames.Inicio) SceneFlow.Quit();
                else SceneFlow.Load(SceneNames.Inicio);
            }

            radius = 150f * baseRt.lossyScale.x;
            foreach (var t in ETouch.activeTouches) HandleTouch(t);
            EditorInput();

            pitch = Mathf.Clamp(pitch, -75f, 75f);
            head.localRotation = Quaternion.Euler(pitch, yaw, 0f);

            float v = speed * (running ? RunMultiplier : 1f);
            Vector3 dir = Quaternion.Euler(0f, rig.eulerAngles.y + yaw, 0f) * new Vector3(moveInput.x, 0f, moveInput.y);
            if (cc.enabled) cc.SimpleMove(dir * v);
            knobRt.anchoredPosition = moveInput * 150f;

            UpdateButtons();
            if (hint != null && Time.time - hintTime > 6f)
                hint.alpha = Mathf.MoveTowards(hint.alpha, 0f, Time.deltaTime);
        }

        void LateUpdate()
        {
            // Por si algún otro componente movió la cámara, dejar la orientación del joystick.
            if (head != null) head.localRotation = Quaternion.Euler(pitch, yaw, 0f);
        }

        bool TryButton(Vector2 pos)
        {
            foreach (var b in buttons)
                if (b.rt.gameObject.activeInHierarchy && RectTransformUtility.RectangleContainsScreenPoint(b.rt, pos, null))
                {
                    b.onTap();
                    b.rt.localScale = Vector3.one * 0.85f;
                    return true;
                }
            return false;
        }

        void HandleTouch(ETouch t)
        {
            int id = t.finger.index;
            Vector2 pos = t.screenPosition;
            switch (t.phase)
            {
                case UnityEngine.InputSystem.TouchPhase.Began:
                    if (TryButton(pos)) { buttonFingers.Add(id); break; }
                    if (moveFinger < 0 && pos.x < Screen.width * 0.35f && pos.y < Screen.height * 0.6f) moveFinger = id;
                    else if (lookFinger < 0) { lookFinger = id; lookMoved = false; }
                    if (id == moveFinger) moveInput = Vector2.ClampMagnitude((pos - (Vector2)baseRt.position) / radius, 1f);
                    break;
                case UnityEngine.InputSystem.TouchPhase.Moved:
                case UnityEngine.InputSystem.TouchPhase.Stationary:
                    if (buttonFingers.Contains(id)) break;
                    if (id == moveFinger) moveInput = Vector2.ClampMagnitude((pos - (Vector2)baseRt.position) / radius, 1f);
                    else if (id == lookFinger)
                    {
                        Vector2 d = t.delta;
                        yaw += d.x * lookSensitivity;
                        pitch -= d.y * lookSensitivity;
                        if ((pos - t.startScreenPosition).magnitude > 25f) lookMoved = true;
                    }
                    break;
                case UnityEngine.InputSystem.TouchPhase.Ended:
                case UnityEngine.InputSystem.TouchPhase.Canceled:
                    if (buttonFingers.Remove(id)) break;
                    if (id == moveFinger) { moveFinger = -1; moveInput = Vector2.zero; }
                    else if (id == lookFinger)
                    {
                        if (!lookMoved && t.time - t.startTime < 0.3) TapThisFrame = true;
                        lookFinger = -1;
                    }
                    break;
            }
        }

        void EditorInput()
        {
            if (!Application.isEditor) return;
            var k = Keyboard.current;
            var m = Mouse.current;
            if (k != null && moveFinger < 0)
            {
                Vector2 v = Vector2.zero;
                if (k.wKey.isPressed) v.y += 1f;
                if (k.sKey.isPressed) v.y -= 1f;
                if (k.dKey.isPressed) v.x += 1f;
                if (k.aKey.isPressed) v.x -= 1f;
                moveInput = Vector2.ClampMagnitude(v, 1f);
                if (k.leftShiftKey.wasPressedThisFrame) running = !running;
                if (k.eKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame) DoAction();
            }
            if (m != null)
            {
                if (m.rightButton.isPressed)
                {
                    Vector2 d = m.delta.ReadValue();
                    yaw += d.x * lookSensitivity;
                    pitch -= d.y * lookSensitivity;
                }
                if (m.leftButton.wasPressedThisFrame && !TryButton(m.position.ReadValue())) TapThisFrame = true;
            }
        }

        // ------------------------------------------------------------------ acciones

        void DoAction()
        {
            var gp = GazePointer.Instance;
            if (gp != null) gp.ActivateCurrent();
        }

        void UpdateButtons()
        {
            var gp = GazePointer.Instance;
            var cur = gp != null ? gp.Current : null;
            actionText.text = cur == null ? "ACCIÓN"
                : cur is Grabbable ? "AGARRAR"
                : cur is PlacementSocket ? "COLOCAR"
                : cur is TeleportPoint ? "IR"
                : cur is Ticket ? "RECOGER"
                : cur is ExhibitAnimator ? "ANIMAR"
                : "PULSAR";
            actionImg.color = cur != null ? new Color(0.35f, 0.85f, 0.95f, 0.85f) : new Color(1f, 1f, 1f, 0.28f);
            runImg.color = running ? new Color(0.98f, 0.72f, 0.30f, 0.9f) : new Color(1f, 1f, 1f, 0.28f);
            foreach (var b in buttons)
                b.rt.localScale = Vector3.MoveTowards(b.rt.localScale, Vector3.one, Time.deltaTime * 2f);
        }
    }
}
