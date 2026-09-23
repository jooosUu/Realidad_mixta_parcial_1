using System;
using UnityEngine;

namespace MuseoAutista
{
    public enum ButtonAction { None, LoadScene, Quit, Recenter }

    /// <summary>Botón 3D que se pulsa con la mirada.</summary>
    public class GazeButton : GazeInteractable
    {
        public ButtonAction action;
        public string sceneName;
        public Renderer background;
        public Color normalColor = new Color(0.20f, 0.42f, 0.47f);
        public Color hoverColor = new Color(0.30f, 0.58f, 0.63f);

        public event Action Activated;

        MaterialPropertyBlock block;
        Vector3 baseScale;
        float blend;

        void Awake()
        {
            baseScale = transform.localScale;
            block = new MaterialPropertyBlock();
            ApplyColor(normalColor);
        }

        void OnDisable()
        {
            blend = 0f;
            transform.localScale = baseScale;
            ApplyColor(normalColor);
        }

        void Update()
        {
            float target = IsHovered ? 1f : 0f;
            if (Mathf.Approximately(blend, target)) return;
            blend = Mathf.MoveTowards(blend, target, Time.deltaTime * 6f);
            transform.localScale = baseScale * (1f + 0.06f * blend);
            ApplyColor(Color.Lerp(normalColor, hoverColor, blend));
        }

        void ApplyColor(Color c)
        {
            if (background == null) return;
            background.GetPropertyBlock(block);
            block.SetColor("_Color", c);
            background.SetPropertyBlock(block);
        }

        public override void OnGazeActivate()
        {
            Sfx.Play("click");
            Activated?.Invoke();
            switch (action)
            {
                case ButtonAction.LoadScene: SceneFlow.Load(sceneName); break;
                case ButtonAction.Quit: SceneFlow.Quit(); break;
                case ButtonAction.Recenter: CardboardManager.Recenter(); break;
            }
        }
    }
}
