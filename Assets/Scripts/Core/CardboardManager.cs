using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
#if !UNITY_EDITOR
using Google.XR.Cardboard;
#endif

namespace MuseoAutista
{
    /// <summary>
    /// Integración con Google Cardboard: parámetros del visor (QR), botones de
    /// cerrar/configurar, recentrado y pantalla siempre encendida.
    /// </summary>
    public class CardboardManager : MonoBehaviour
    {
        static bool askedForViewer;

        void Start()
        {
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Application.targetFrameRate = 60;
            if (AppMode.Joystick)
            {
                // Versión pantalla completa: sin Cardboard, con joystick virtual.
                gameObject.AddComponent<JoystickControls>();
                return;
            }
#if !UNITY_EDITOR
            if (!askedForViewer && !Api.HasDeviceParams())
            {
                askedForViewer = true;
                Api.ScanDeviceParams();
            }
#endif
            StartCoroutine(RecenterNextFrame());
        }

        IEnumerator RecenterNextFrame()
        {
            yield return null;
            Recenter();
        }

        public static void Recenter()
        {
            if (AppMode.Joystick) return;
#if !UNITY_EDITOR
            Api.Recenter();
#endif
        }

#if !UNITY_EDITOR
        void Update()
        {
            if (AppMode.Joystick) return;
            if (Api.IsGearButtonPressed) Api.ScanDeviceParams();

            if (Api.IsCloseButtonPressed)
            {
                if (SceneManager.GetActiveScene().name == SceneNames.Inicio) SceneFlow.Quit();
                else SceneFlow.Load(SceneNames.Inicio);
            }

            if (Api.IsTriggerHeldPressed) Api.Recenter();
            if (Api.HasNewDeviceParams()) Api.ReloadDeviceParams();
            Api.UpdateScreenParams();
        }
#endif
    }
}
