using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MuseoAutista
{
    public static class SceneNames
    {
        public const string Inicio = "Inicio";
        public const string Tutorial = "Tutorial";
        public const string Recorrido = "Recorrido";
        public const string Felicitaciones = "Felicitaciones";
    }

    /// <summary>Cambio de escena con fundido y salida de la aplicación.</summary>
    public static class SceneFlow
    {
        static bool loading;

        public static void Load(string scene)
        {
            if (loading || string.IsNullOrEmpty(scene)) return;
            var rig = PlayerRig.Instance;
            if (rig != null && rig.fader != null)
                rig.StartCoroutine(LoadRoutine(rig.fader, scene));
            else
                SceneManager.LoadScene(scene);
        }

        static IEnumerator LoadRoutine(ScreenFader fader, string scene)
        {
            loading = true;
            yield return fader.Fade(1f, 0.4f);
            loading = false;
            SceneManager.LoadScene(scene);
        }

        public static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
