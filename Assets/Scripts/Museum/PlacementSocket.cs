using System;
using UnityEngine;
using UnityEngine.UI;

namespace MuseoAutista
{
    /// <summary>
    /// Pedestal donde se posiciona una pieza. Solo acepta la pieza correcta;
    /// si es otra, avisa en qué sala va.
    /// </summary>
    public class PlacementSocket : GazeInteractable
    {
        [Tooltip("itemId aceptado. Vacío = acepta cualquier pieza.")]
        public string acceptsItemId;
        public Transform anchor;
        public Renderer glow;
        public Text statusText;
        public string waitingMessage = "Trae la pieza y mira aquí para colocarla";
        public Color idleColor = new Color(0.55f, 0.55f, 0.58f);
        public Color readyColor = new Color(0.95f, 0.80f, 0.35f);
        public Color doneColor = new Color(0.45f, 0.80f, 0.50f);
        public Color errorColor = new Color(0.85f, 0.40f, 0.35f);

        public bool Filled { get; private set; }
        public static event Action<PlacementSocket> AnyFilled;

        MaterialPropertyBlock block;
        float errorTimer;

        void Awake()
        {
            block = new MaterialPropertyBlock();
            if (statusText != null) statusText.text = waitingMessage;
        }

        public override bool IsInteractable => base.IsInteractable && !Filled && Grabbable.Held != null;

        void Update()
        {
            if (glow == null) return;
            Color c;
            if (Filled) c = doneColor;
            else if (errorTimer > 0f) { errorTimer -= Time.deltaTime; c = errorColor; }
            else if (Grabbable.Held != null)
                c = Color.Lerp(idleColor, readyColor, 0.5f + 0.5f * Mathf.Sin(Time.time * 4f));
            else c = idleColor;

            glow.GetPropertyBlock(block);
            block.SetColor("_Color", c);
            block.SetColor("_EmissionColor", c * 0.5f);
            glow.SetPropertyBlock(block);
        }

        public override void OnGazeActivate()
        {
            var item = Grabbable.Held;
            if (item == null || Filled) return;

            if (string.IsNullOrEmpty(acceptsItemId) || item.itemId == acceptsItemId)
            {
                item.PlaceAt(anchor != null ? anchor : transform);
                Filled = true;
                Sfx.Play("place", 0.6f);
                if (statusText != null) statusText.text = "¡" + item.displayName + " en su lugar!";
                AnyFilled?.Invoke(this);
            }
            else
            {
                errorTimer = 1.2f;
                Sfx.Play("error");
                if (statusText != null)
                    statusText.text = "Esta pieza no va aquí.\n" + item.displayName + " va en la " + item.hallName + ".";
            }
        }
    }
}
