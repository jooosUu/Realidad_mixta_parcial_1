using System;
using System.Collections;
using UnityEngine;

namespace MuseoAutista
{
    /// <summary>Raíz del jugador: cámara Cardboard, punto de agarre y fundido.</summary>
    public class PlayerRig : MonoBehaviour
    {
        public static PlayerRig Instance { get; private set; }

        public Transform head;
        public Transform holdPoint;
        public ScreenFader fader;
        public TeleportPoint startPoint;

        public TeleportPoint Current { get; private set; }

        public static event Action<TeleportPoint> Teleported;

        bool busy;

        void Awake()
        {
            Instance = this;
            Current = startPoint;
        }

        void Start()
        {
            if (startPoint != null) GameState.VisitHall(startPoint.hallId);
            if (fader != null) StartCoroutine(fader.Fade(0f, 0.6f));
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void TeleportTo(TeleportPoint point)
        {
            if (busy || point == null) return;
            StartCoroutine(TeleportRoutine(point));
        }

        IEnumerator TeleportRoutine(TeleportPoint point)
        {
            busy = true;
            Sfx.Play("teleport");
            if (fader != null) yield return fader.Fade(1f, 0.15f);
            Vector3 p = point.transform.position;
            // En la versión joystick hay CharacterController: apagarlo para poder mover el jugador.
            var cc = GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            transform.position = new Vector3(p.x, transform.position.y, p.z);
            if (cc != null) cc.enabled = true;
            Current = point;
            GameState.VisitHall(point.hallId);
            Teleported?.Invoke(point);
            if (fader != null) yield return fader.Fade(0f, 0.25f);
            busy = false;
        }
    }
}
