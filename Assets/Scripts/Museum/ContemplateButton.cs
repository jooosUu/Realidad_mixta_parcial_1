using UnityEngine;
using UnityEngine.UI;

namespace MuseoAutista
{
    /// <summary>Botón "Contemplar" de una placa: activa/desactiva el modo Contemplar de su pieza.</summary>
    [RequireComponent(typeof(GazeButton))]
    public class ContemplateButton : MonoBehaviour
    {
        public ExhibitAnimator target;
        Text label;
        bool shownActive;

        void Awake()
        {
            label = GetComponentInChildren<Text>();
            GetComponent<GazeButton>().Activated += () => Contemplation.Toggle(target);
            Refresh(false);
        }

        void Update()
        {
            bool on = Contemplation.Active == target && target != null;
            if (on != shownActive) Refresh(on);
        }

        void Refresh(bool on)
        {
            shownActive = on;
            if (label != null) label.text = on ? "DEJAR DE CONTEMPLAR" : "CONTEMPLAR";
        }
    }
}
