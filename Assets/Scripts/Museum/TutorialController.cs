using System;
using UnityEngine;
using UnityEngine.UI;

namespace MuseoAutista
{
    /// <summary>
    /// Tutorial guiado paso a paso: mirar un botón, teletransportarse, agarrar,
    /// colocar y animar un modelo. El panel se reubica frente al jugador en cada paso.
    /// </summary>
    public class TutorialController : MonoBehaviour
    {
        [Serializable]
        public class Step
        {
            public string title;
            [TextArea] public string body;
            public Transform panelAnchor;
            public GameObject[] enable;
        }

        public Step[] steps;
        public Transform panel;
        public Text titleText;
        public Text bodyText;
        public Text stepCounter;
        public GazeButton nextButton;
        public GameObject finalButtons;
        public TeleportPoint teleportTarget;

        int index = -1;

        void OnEnable()
        {
            PlayerRig.Teleported += OnTeleported;
            Grabbable.PickedUp += OnPicked;
            PlacementSocket.AnyFilled += OnPlaced;
            ExhibitAnimator.AnyPlayed += OnPlayed;
            if (nextButton) nextButton.Activated += Next;
        }

        void OnDisable()
        {
            PlayerRig.Teleported -= OnTeleported;
            Grabbable.PickedUp -= OnPicked;
            PlacementSocket.AnyFilled -= OnPlaced;
            ExhibitAnimator.AnyPlayed -= OnPlayed;
            if (nextButton) nextButton.Activated -= Next;
        }

        void Start()
        {
            foreach (var s in steps)
                foreach (var go in s.enable) if (go) go.SetActive(false);
            if (finalButtons) finalButtons.SetActive(false);
            Next();
        }

        // Pasos: 0 mirar botón, 1 teletransporte, 2 agarrar, 3 colocar, 4 animar, 5 fin.
        void OnTeleported(TeleportPoint p) { if (index == 1 && p == teleportTarget) Next(); }
        void OnPicked(Grabbable g) { if (index == 2) Next(); }
        void OnPlaced(PlacementSocket s) { if (index == 3) Next(); }
        void OnPlayed(ExhibitAnimator e) { if (index == 4) Invoke(nameof(Next), 2.5f); }

        public void Next()
        {
            if (index >= steps.Length - 1) return;
            index++;
            var step = steps[index];
            foreach (var go in step.enable) if (go) go.SetActive(true);

            titleText.text = TextUtil.Doom(step.title);
            bodyText.text = step.body;
            stepCounter.text = "Paso " + (index + 1) + " de " + steps.Length;
            if (nextButton) nextButton.gameObject.SetActive(index == 0);
            if (finalButtons) finalButtons.SetActive(index == steps.Length - 1);

            if (step.panelAnchor) panel.SetPositionAndRotation(step.panelAnchor.position, step.panelAnchor.rotation);
            if (index > 0) Sfx.Play("place", 0.35f);
        }
    }
}
