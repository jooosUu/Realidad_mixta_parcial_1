using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MuseoAutista.EditorTools
{
    /// <summary>Prueba automática en modo juego de la versión joystick (apuntar a una pieza y usar el botón de acción).</summary>
    public static class JoystickPlayTest
    {
        static int frame;
        static int step;

        public static void Run()
        {
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload | EnterPlayModeOptions.DisableSceneReload;
            AppMode.ForceJoystick = true;
            EditorSceneManager.OpenScene("Assets/Scenes/Recorrido.unity");
            Application.logMessageReceived += (msg, stack, type) =>
            {
                if (type == LogType.Exception || type == LogType.Error) System.Console.WriteLine("[PT-ERR] " + msg + "\n" + stack);
            };
            EditorApplication.update += Tick;
            EditorApplication.isPlaying = true;
        }

        static void Log(string s) { Debug.Log("[PT] " + s); System.Console.WriteLine("[PT] " + s); }

        static void Tick()
        {
            if (!EditorApplication.isPlaying) return;
            // En modo batch el juego no avanza solo: pausar y avanzar cuadro a cuadro.
            if (!EditorApplication.isPaused) EditorApplication.isPaused = true;
            EditorApplication.Step();
            frame++;
            var jc = Object.FindAnyObjectByType<JoystickControls>();
            var gp = GazePointer.Instance;
            if (frame == 60)
            {
                Log("JoystickControls: " + (jc != null) + "  GazePointer: " + (gp != null) + "  cam: " + (Camera.main ? Camera.main.transform.rotation.eulerAngles.ToString() : "null"));
                var item = Object.FindAnyObjectByType<Grabbable>();
                Vector3 eye = gp.transform.position;
                Vector3 d = (item.transform.position + Vector3.up * 0.15f - eye).normalized;
                float yaw = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
                float pitch = -Mathf.Asin(d.y) * Mathf.Rad2Deg;
                var t = typeof(JoystickControls);
                t.GetField("yaw", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(jc, yaw);
                t.GetField("pitch", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(jc, pitch);
                Log("apuntando a " + item.itemId + " yaw " + yaw + " pitch " + pitch);
            }
            if (frame == 61 || frame == 62 || frame == 90)
            {
                var f = BindingFlags.NonPublic | BindingFlags.Instance;
                var t2 = typeof(JoystickControls);
                Log("f" + frame + " rig=" + t2.GetField("rig", f).GetValue(jc) + " head=" + t2.GetField("head", f).GetValue(jc)
                    + " yaw=" + t2.GetField("yaw", f).GetValue(jc) + " enabled=" + jc.enabled + " active=" + jc.isActiveAndEnabled
                    + " headRot=" + PlayerRig.Instance.head.localEulerAngles + " time=" + Time.time + " dt=" + Time.deltaTime);
            }
            if (frame == 90)
            {
                Log("cam forward: " + gp.transform.forward + " rot: " + gp.transform.localRotation.eulerAngles + "  Current: " + (gp.Current ? gp.Current.name : "null") + "  Hit: " + (gp.HitTransform ? gp.HitTransform.name : "null"));
                typeof(JoystickControls).GetMethod("DoAction", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(jc, null);
            }
            if (frame == 130)
            {
                Log("Grabbable.Held: " + (Grabbable.Held ? Grabbable.Held.itemId : "null"));
                EditorApplication.isPlaying = false;
                EditorApplication.update -= Tick;
                EditorApplication.Exit(0);
            }
        }
    }
}
