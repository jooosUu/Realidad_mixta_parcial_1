using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using UnityEngine.XR.Management;

namespace MuseoAutista
{
    /// <summary>
    /// Control de vista de respaldo cuando Cardboard XR no está activo:
    /// - Editor: mantener clic derecho y mover el ratón.
    /// - Celular sin XR: usa el sensor de orientación (giroscopio) y, si no hay,
    ///   arrastrar el dedo por la pantalla.
    /// Con Cardboard activo no hace nada (la cámara la mueve el TrackedPoseDriver).
    /// </summary>
    public class EditorMouseLook : MonoBehaviour
    {
        public float sensitivity = 0.15f;
        float yaw, pitch;
        bool attitudeReady;
        Quaternion attitudeOffset = Quaternion.identity;
        bool calibrated;

        public static bool XRActive
        {
            get
            {
                var s = XRGeneralSettings.Instance;
                return s != null && s.Manager != null && s.Manager.activeLoader != null && XRSettings.isDeviceActive;
            }
        }

        void Awake()
        {
            Vector3 e = transform.localEulerAngles;
            yaw = e.y;
            pitch = e.x > 180f ? e.x - 360f : e.x;
        }

        void Update()
        {
            if (Application.isEditor) { MouseLook(); return; }
            if (XRActive) return;
            if (AttitudeLook()) return;
            TouchLook();
        }

        void MouseLook()
        {
            var mouse = Mouse.current;
            if (mouse == null || !mouse.rightButton.isPressed) return;
            Vector2 delta = mouse.delta.ReadValue();
            yaw += delta.x * sensitivity;
            pitch = Mathf.Clamp(pitch - delta.y * sensitivity, -80f, 80f);
            transform.localRotation = Quaternion.Euler(pitch, yaw, 0f);
        }

        bool AttitudeLook()
        {
            var sensor = AttitudeSensor.current;
            if (sensor == null) return false;
            if (!sensor.enabled) InputSystem.EnableDevice(sensor);
            Quaternion q = sensor.attitude.ReadValue();
            if (q == Quaternion.identity && !attitudeReady) return false;
            attitudeReady = true;
            // Convierte la orientación del dispositivo (mano derecha) al sistema de Unity en horizontal.
            Quaternion unity = Quaternion.Euler(90f, 0f, 0f) * new Quaternion(q.x, q.y, -q.z, -q.w);
            if (!calibrated)
            {
                attitudeOffset = Quaternion.Euler(0f, -unity.eulerAngles.y, 0f);
                calibrated = true;
            }
            transform.localRotation = attitudeOffset * unity;
            return true;
        }

        void TouchLook()
        {
            var ts = Touchscreen.current;
            if (ts == null || !ts.primaryTouch.press.isPressed) return;
            Vector2 delta = ts.primaryTouch.delta.ReadValue();
            yaw -= delta.x * 0.12f;
            pitch = Mathf.Clamp(pitch + delta.y * 0.12f, -80f, 80f);
            transform.localRotation = Quaternion.Euler(pitch, yaw, 0f);
        }
    }
}
