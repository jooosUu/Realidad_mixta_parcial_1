using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MuseoAutista
{
    /// <summary>
    /// Puntero Gaze: lanza un rayo desde el centro de la vista, muestra un retículo
    /// de alto contraste a la profundidad del objeto mirado y activa los
    /// GazeInteractable al completar el tiempo de mirada o con el gatillo del visor.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class GazePointer : MonoBehaviour
    {
        public float maxDistance = 40f;
        public float idleDistance = 3f;
        [Tooltip("Tamaño angular del retículo (se escala con la distancia).")]
        public float angularScale = 0.00035f;

        [Header("Retículo")]
        public Transform reticle;
        public Image dot;
        public Image ring;
        public Image fill;

        GazeInteractable current;
        float timer;
        bool fired;
        float hoverBlend;

        public GazeInteractable Current => current;
        /// <summary>Objeto que el rayo está tocando (interactivo o no).</summary>
        public Transform HitTransform { get; private set; }

        void Update()
        {
            Ray ray = new Ray(transform.position, transform.forward);
            float distance = idleDistance;
            GazeInteractable target = null;
            HitTransform = null;

            if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, ~0, QueryTriggerInteraction.Collide))
            {
                distance = hit.distance;
                HitTransform = hit.transform;
                target = hit.collider.GetComponentInParent<GazeInteractable>();
                if (target != null && !target.IsInteractable) target = null;
            }

            if (target != current)
            {
                if (current != null) current.OnGazeExit();
                current = target;
                timer = 0f;
                fired = false;
                if (current != null) current.OnGazeEnter();
            }

            if (current != null)
            {
                bool trigger = TriggerPressed();
                if (!fired) timer += Time.deltaTime;
                if (trigger || (!fired && timer >= current.dwellTime))
                {
                    fired = true;
                    timer = current.dwellTime;
                    current.OnGazeActivate();
                }
            }

            UpdateReticle(ray, distance);
        }

        void UpdateReticle(Ray ray, float distance)
        {
            if (reticle == null) return;
            float d = Mathf.Max(0.35f, distance * 0.97f);
            reticle.position = ray.origin + ray.direction * d;
            reticle.rotation = Quaternion.LookRotation(ray.direction, transform.up);
            reticle.localScale = Vector3.one * (d * angularScale);

            bool hovering = current != null;
            hoverBlend = Mathf.MoveTowards(hoverBlend, hovering ? 1f : 0f, Time.deltaTime * 6f);
            if (ring != null)
            {
                ring.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.35f, 1f, hoverBlend);
                var c = ring.color; c.a = hoverBlend; ring.color = c;
            }
            if (fill != null)
            {
                fill.fillAmount = hovering && current.dwellTime > 0f ? Mathf.Clamp01(timer / current.dwellTime) : 0f;
            }
            if (dot != null)
            {
                dot.rectTransform.localScale = Vector3.one * Mathf.Lerp(1f, 0.7f, hoverBlend);
            }
        }

        static bool TriggerPressed()
        {
#if UNITY_EDITOR
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) return true;
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) return true;
            return false;
#else
            if (EditorMouseLook.XRActive) return Google.XR.Cardboard.Api.IsTriggerPressed;
            // Sin XR: un toque en la pantalla hace de gatillo.
            var ts = Touchscreen.current;
            return ts != null && ts.primaryTouch.press.wasPressedThisFrame;
#endif
        }
    }
}
