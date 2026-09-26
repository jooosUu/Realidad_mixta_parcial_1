using UnityEngine;
using UnityEngine.UI;

namespace MuseoAutista
{
    /// <summary>Muestra el resumen del recorrido en la escena de felicitaciones.</summary>
    public class CongratsScreen : MonoBehaviour
    {
        public Text statsText;

        void Start()
        {
            Sfx.Play("success", 0.6f);
            if (statsText == null) return;
            if (GameState.Finished)
            {
                int s = Mathf.RoundToInt(GameState.FinalSeconds);
                statsText.text = "Tiempo del recorrido: " + (s / 60) + " min " + (s % 60).ToString("00") + " s\n" +
                                 "Piezas devueltas: " + GameState.PiecesPlaced + "/" + GameState.PiecesTotal + "\n" +
                                 "Salas visitadas: " + GameState.HallsVisited + "/" + GameState.Halls.Length +
                                 (GameState.TicketsTotal > 0 ? "\nTickets dorados: " + GameState.TicketsCollected + "/" + GameState.TicketsTotal : "");
            }
            else
            {
                statsText.text = "Recorriste el Museo Autista.\n¡Gracias por tu visita!";
            }
        }
    }
}
