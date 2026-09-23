using UnityEngine;

namespace MuseoAutista
{
    /// <summary>Animación ambiental: giro continuo y/o flotación suave.</summary>
    public class SimpleMotion : MonoBehaviour
    {
        public Vector3 spinAxis = Vector3.up;
        public float spinSpeed;
        public float bobAmplitude;
        public float bobFrequency = 0.5f;

        Vector3 basePos;
        float phase;

        void Awake()
        {
            basePos = transform.localPosition;
            phase = Random.value * 6f;
        }

        void Update()
        {
            if (spinSpeed != 0f) transform.Rotate(spinAxis, spinSpeed * Time.deltaTime, Space.Self);
            if (bobAmplitude != 0f)
                transform.localPosition = basePos + Vector3.up * Mathf.Sin((Time.time + phase) * bobFrequency * 2f * Mathf.PI) * bobAmplitude;
        }
    }
}
