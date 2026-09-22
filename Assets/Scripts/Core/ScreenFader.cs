using System.Collections;
using UnityEngine;

namespace MuseoAutista
{
    /// <summary>Quad negro delante de la cámara para fundidos (teletransporte y cambio de escena).</summary>
    public class ScreenFader : MonoBehaviour
    {
        public Renderer quad;
        float alpha = 1f;
        MaterialPropertyBlock block;

        void Awake()
        {
            block = new MaterialPropertyBlock();
            Apply();
        }

        public IEnumerator Fade(float target, float duration)
        {
            float start = alpha;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                alpha = Mathf.Lerp(start, target, Mathf.Clamp01(t / duration));
                Apply();
                yield return null;
            }
            alpha = target;
            Apply();
        }

        void Apply()
        {
            if (quad == null) return;
            quad.enabled = alpha > 0.001f;
            quad.GetPropertyBlock(block);
            block.SetColor("_Color", new Color(0f, 0f, 0f, alpha));
            quad.SetPropertyBlock(block);
        }
    }
}
