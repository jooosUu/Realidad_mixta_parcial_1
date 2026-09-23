using UnityEngine;

namespace MuseoAutista
{
    /// <summary>
    /// Punto de salto en el suelo. Al mirarlo el jugador se teletransporta con un
    /// fundido corto (evita mareo). Se oculta mientras el jugador está encima.
    /// </summary>
    public class TeleportPoint : GazeInteractable
    {
        [Tooltip("Identificador de la sala (para contar salas visitadas).")]
        public string hallId;
        public Transform visual;
        public Renderer ring;
        public Color idleColor = new Color(0.35f, 0.75f, 0.85f);
        public Color hoverColor = new Color(0.95f, 0.80f, 0.35f);

        MaterialPropertyBlock block;
        Vector3 baseScale;

        void Awake()
        {
            block = new MaterialPropertyBlock();
            if (visual != null) baseScale = visual.localScale;
        }

        bool Occupied => PlayerRig.Instance != null && PlayerRig.Instance.Current == this;

        public override bool IsInteractable => base.IsInteractable && !Occupied;

        void Update()
        {
            if (visual == null) return;
            bool show = !Occupied;
            if (visual.gameObject.activeSelf != show) visual.gameObject.SetActive(show);
            if (!show) return;

            float pulse = 1f + 0.06f * Mathf.Sin(Time.time * 2.5f);
            visual.localScale = baseScale * (IsHovered ? 1.15f : pulse);

            if (ring != null)
            {
                Color c = IsHovered ? hoverColor : idleColor;
                ring.GetPropertyBlock(block);
                block.SetColor("_Color", c);
                block.SetColor("_EmissionColor", c * 0.6f);
                ring.SetPropertyBlock(block);
            }
        }

        public override void OnGazeActivate()
        {
            if (PlayerRig.Instance != null) PlayerRig.Instance.TeleportTo(this);
        }
    }
}
