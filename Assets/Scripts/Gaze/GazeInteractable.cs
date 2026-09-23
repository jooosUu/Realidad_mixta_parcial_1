using UnityEngine;

namespace MuseoAutista
{
    /// <summary>
    /// Base de todo objeto que responde a la mirada (Gaze).
    /// El GazePointer llama a Enter/Exit y, al completar el tiempo de mirada
    /// (o al tocar la pantalla / botón del visor Cardboard), a Activate.
    /// </summary>
    public abstract class GazeInteractable : MonoBehaviour
    {
        [Tooltip("Segundos que hay que mirar el objeto para activarlo.")]
        public float dwellTime = 1.5f;

        public bool IsHovered { get; private set; }

        public virtual bool IsInteractable => isActiveAndEnabled;

        public virtual void OnGazeEnter() { IsHovered = true; }

        public virtual void OnGazeExit() { IsHovered = false; }

        public abstract void OnGazeActivate();
    }
}
