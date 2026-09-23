using UnityEngine;

namespace MuseoAutista
{
    /// <summary>
    /// Luces de una sala: lámparas del techo, tiras de luz del color de la sala y haces de
    /// luz sobre cada pieza. Se encienden al entrar (teletransportarse a un punto de la
    /// sala) y se apagan suavemente al salir.
    /// </summary>
    public class HallLights : MonoBehaviour
    {
        public string hallId;
        public Color hallColor = Color.white;
        public Renderer[] lamps;
        public Renderer[] strips;
        public Renderer[] beams;

        static readonly Color LampOn = new Color(1f, 0.97f, 0.9f);
        static readonly Color LampOff = new Color(0.55f, 0.55f, 0.55f);

        float level, target;
        MaterialPropertyBlock block;

        void OnEnable() { PlayerRig.Teleported += OnTeleported; }
        void OnDisable() { PlayerRig.Teleported -= OnTeleported; }

        void Start()
        {
            block = new MaterialPropertyBlock();
            var cur = PlayerRig.Instance != null ? PlayerRig.Instance.Current : null;
            target = level = cur != null && cur.hallId == hallId ? 1f : 0f;
            Apply();
        }

        void OnTeleported(TeleportPoint p)
        {
            target = p != null && p.hallId == hallId ? 1f : 0f;
        }

        void Update()
        {
            if (Mathf.Approximately(level, target)) return;
            level = Mathf.MoveTowards(level, target, Time.deltaTime * (target > level ? 0.9f : 1.5f));
            Apply();
        }

        void Apply()
        {
            // Encendido con un pequeño parpadeo al principio, como fluorescentes.
            float flicker = level > 0f && level < 0.35f ? (Mathf.PerlinNoise(Time.time * 25f, 0f) > 0.45f ? 1f : 0.3f) : 1f;
            float l = level * flicker;

            Set(lamps, Color.Lerp(LampOff, LampOn, l), LampOn * l);
            Set(strips, Color.Lerp(hallColor * 0.35f, Color.Lerp(hallColor, Color.white, 0.3f), l), hallColor * 1.3f * l);
            foreach (var r in beams)
            {
                if (r == null) continue;
                r.enabled = l > 0.01f;
                r.GetPropertyBlock(block);
                block.SetColor("_Color", Color.Lerp(hallColor, Color.white, 0.55f) * 0.2f * l);
                r.SetPropertyBlock(block);
            }
        }

        void Set(Renderer[] rs, Color baseColor, Color emission)
        {
            foreach (var r in rs)
            {
                if (r == null) continue;
                r.GetPropertyBlock(block);
                block.SetColor("_Color", baseColor);
                block.SetColor("_EmissionColor", emission);
                r.SetPropertyBlock(block);
            }
        }
    }
}
