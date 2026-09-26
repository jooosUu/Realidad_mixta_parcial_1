using UnityEngine;
using UnityEngine.UI;

namespace MuseoAutista
{
    /// <summary>
    /// Misión del recorrido: devolver las 4 piezas a su sala. Al completarla se
    /// habilitan los botones para ir a la pantalla de felicitaciones.
    /// </summary>
    public class MuseumProgress : MonoBehaviour
    {
        public PlacementSocket[] sockets;
        public Text[] progressTexts;
        public GameObject[] finishButtons;
        public int ticketsTotal;

        void OnEnable() { PlacementSocket.AnyFilled += OnFilled; Ticket.Collected += Refresh; }
        void OnDisable() { PlacementSocket.AnyFilled -= OnFilled; Ticket.Collected -= Refresh; }

        void Awake() { GameState.StartTour(); }

        void Start()
        {
            GameState.PiecesTotal = sockets.Length;
            GameState.TicketsTotal = ticketsTotal;
            foreach (var b in finishButtons) if (b) b.SetActive(false);
            Refresh();
        }

        void OnFilled(PlacementSocket socket)
        {
            int filled = 0;
            foreach (var s in sockets) if (s && s.Filled) filled++;
            GameState.PiecesPlaced = filled;
            Refresh();
            if (filled >= sockets.Length)
            {
                GameState.FinishTour();
                Sfx.Play("success", 0.6f);
                foreach (var b in finishButtons) if (b) b.SetActive(true);
            }
        }

        void Refresh()
        {
            int placed = GameState.PiecesPlaced;
            string msg = placed >= sockets.Length
                ? "¡Misión completa! " + placed + "/" + sockets.Length + " piezas.\nMira \"Finalizar recorrido\"."
                : "Piezas en su lugar: " + placed + "/" + sockets.Length;
            if (ticketsTotal > 0) msg += "\nTickets dorados: " + GameState.TicketsCollected + "/" + ticketsTotal;
            foreach (var t in progressTexts) if (t) t.text = msg;
        }
    }
}
