using System.Collections.Generic;
using UnityEngine;

namespace MuseoAutista
{
    /// <summary>Datos del recorrido que se muestran en la pantalla de felicitaciones.</summary>
    public static class GameState
    {
        public static readonly string[] Halls = { "aviones", "dinosaurios", "hongos", "carros" };

        static readonly HashSet<string> visited = new HashSet<string>();
        static float startTime;

        public static int PiecesPlaced { get; set; }
        public static int PiecesTotal { get; set; } = 4;
        public static int TicketsCollected { get; set; }
        public static int TicketsTotal { get; set; }
        public static float FinalSeconds { get; private set; }
        public static bool Finished { get; private set; }

        public static int HallsVisited
        {
            get
            {
                int n = 0;
                foreach (var h in Halls) if (visited.Contains(h)) n++;
                return n;
            }
        }

        public static void StartTour()
        {
            visited.Clear();
            PiecesPlaced = 0;
            TicketsCollected = 0;
            Finished = false;
            startTime = Time.time;
        }

        public static void VisitHall(string hallId)
        {
            if (!string.IsNullOrEmpty(hallId)) visited.Add(hallId);
        }

        public static void FinishTour()
        {
            if (Finished) return;
            Finished = true;
            FinalSeconds = Time.time - startTime;
        }
    }
}
