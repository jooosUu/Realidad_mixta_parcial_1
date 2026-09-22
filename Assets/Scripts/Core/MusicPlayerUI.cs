using UnityEngine;
using UnityEngine.UI;

namespace MuseoAutista
{
    /// <summary>
    /// Reproductor de música en la "esquinita" inferior izquierda de la vista.
    /// Sigue el cuerpo del jugador (no la cabeza) para que se pueda apuntar: solo se
    /// reacomoda cuando el jugador gira mucho. Al mirar el ícono se abre el panel con
    /// anterior / play-pausa / siguiente / música del celular, y se esconde solo
    /// cuando se deja de mirar.
    /// </summary>
    public class MusicPlayerUI : MonoBehaviour
    {
        public Transform head;
        public Transform anchor;
        public GazeButton icon;
        public GameObject panel;
        public GazeButton prev, playPause, next, phone;
        public Image playPauseImage;
        public Sprite playSprite, pauseSprite;
        public Text title;
        public Text status;
        [Header("Pantalla con la portada")]
        public RawImage cover;
        public Texture defaultCover;

        [Tooltip("Dirección del reproductor respecto al frente del cuerpo (grados).")]
        public float yawOffset = -32f;
        public float pitch = 30f;
        public float distance = 1.1f;
        public float hideDelay = 3f;

        float bodyYaw;
        float lastGaze;
        GazePointer pointer;

        void Start()
        {
            bodyYaw = head.eulerAngles.y;
            pointer = head.GetComponent<GazePointer>();
            icon.Activated += Open;
            prev.Activated += () => MusicPlayer.Instance?.Prev();
            next.Activated += () => MusicPlayer.Instance?.Next();
            playPause.Activated += () => MusicPlayer.Instance?.TogglePlay();
            phone.Activated += () => MusicPlayer.Instance?.ScanPhone();
            Close();
            Place(true);
        }

        void Open()
        {
            panel.SetActive(true);
            icon.gameObject.SetActive(false);
            lastGaze = Time.time;
        }

        void Close()
        {
            panel.SetActive(false);
            icon.gameObject.SetActive(true);
        }

        void LateUpdate()
        {
            Place(false);

            if (panel.activeSelf)
            {
                var hit = pointer != null ? pointer.HitTransform : null;
                if (hit != null && hit.IsChildOf(transform)) lastGaze = Time.time;
                if (Time.time - lastGaze > hideDelay) Close();

                var mp = MusicPlayer.Instance;
                if (mp != null)
                {
                    title.text = mp.CurrentTitle;
                    status.text = mp.Status;
                    playPauseImage.sprite = mp.IsPlaying ? pauseSprite : playSprite;
                    if (cover != null)
                    {
                        var tex = mp.CurrentCover;
                        cover.texture = tex != null ? tex : defaultCover;
                        // Pequeño "latido" mientras suena.
                        float beat = mp.IsPlaying ? 1f + Mathf.Abs(Mathf.Sin(Time.time * 3.1f)) * 0.025f : 1f;
                        cover.rectTransform.localScale = Vector3.one * beat;
                    }
                }
            }
        }

        void Place(bool snap)
        {
            // El "cuerpo" solo gira cuando la cabeza se aleja más de 55° (zona muerta).
            float headYaw = head.eulerAngles.y;
            float delta = Mathf.DeltaAngle(bodyYaw, headYaw);
            if (snap) bodyYaw = headYaw;
            else if (Mathf.Abs(delta) > 55f)
                bodyYaw = Mathf.MoveTowardsAngle(bodyYaw, headYaw, (Mathf.Abs(delta) - 50f) * Time.deltaTime * 4f);

            Quaternion dir = Quaternion.Euler(pitch, bodyYaw + yawOffset, 0f);
            Vector3 pos = head.position + dir * Vector3.forward * distance;
            anchor.position = snap ? pos : Vector3.Lerp(anchor.position, pos, 1f - Mathf.Exp(-8f * Time.deltaTime));
            anchor.rotation = Quaternion.LookRotation(anchor.position - head.position, Vector3.up);
        }
    }
}
