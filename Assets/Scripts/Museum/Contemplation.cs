using System.Collections;
using UnityEngine;

namespace MuseoAutista
{
    /// <summary>
    /// Modo Contemplar: oscurece lo que está detrás de la pieza (como un foco de museo),
    /// la hace girar lentamente, reproduce el audio de contemplación y baja la música.
    /// Termina solo a los 20 s o al volver a pulsar el botón.
    /// </summary>
    public class Contemplation : MonoBehaviour
    {
        const float Duration = 20f;
        const float SpinSpeed = 12f;
        const float MaxDim = 0.82f;

        static Contemplation instance;
        public static ExhibitAnimator Active => instance != null ? instance.target : null;

        ExhibitAnimator target;
        Transform dome;
        Material domeMat;
        AudioSource voice;
        Quaternion startRotation;
        float started;
        bool ending;

        public static void Toggle(ExhibitAnimator exhibit)
        {
            if (exhibit == null) return;
            if (instance == null)
            {
                var go = new GameObject("Contemplacion");
                instance = go.AddComponent<Contemplation>();
            }
            if (instance.target == exhibit) instance.End();
            else instance.Begin(exhibit);
        }

        void Awake()
        {
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(sphere.GetComponent<Collider>());
            sphere.name = "DomoOscuro";
            dome = sphere.transform;
            dome.SetParent(transform, false);
            var src = Resources.Load<Material>("ContemplacionDomo");
            domeMat = src != null ? new Material(src) : new Material(Shader.Find("MuseoAutista/Dim"));
            sphere.GetComponent<MeshRenderer>().sharedMaterial = domeMat;
            sphere.SetActive(false);
            voice = gameObject.AddComponent<AudioSource>();
            voice.spatialBlend = 0f;
            voice.playOnAwake = false;
            voice.clip = Resources.Load<AudioClip>("Contemplacion");
        }

        void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        void Begin(ExhibitAnimator exhibit)
        {
            if (target != null) target.transform.rotation = startRotation;
            StopAllCoroutines();
            ending = false;
            target = exhibit;
            startRotation = target.transform.rotation;
            started = Time.time;
            dome.gameObject.SetActive(true);
            if (voice.clip != null) voice.Play();
            if (MusicPlayer.Instance != null) MusicPlayer.Instance.SetDuck(true);
            StartCoroutine(FadeDim(MaxDim, 0.8f));
        }

        void End()
        {
            if (target == null || ending) return;
            ending = true;
            StartCoroutine(EndRoutine());
        }

        IEnumerator EndRoutine()
        {
            var t = target;
            Quaternion from = t.transform.rotation;
            yield return FadeDim(0f, 0.8f, k => t.transform.rotation = Quaternion.Slerp(from, startRotation, k));
            t.transform.rotation = startRotation;
            dome.gameObject.SetActive(false);
            if (MusicPlayer.Instance != null) MusicPlayer.Instance.SetDuck(false);
            target = null;
            ending = false;
        }

        IEnumerator FadeDim(float to, float time, System.Action<float> step = null)
        {
            float from = domeMat.color.a;
            for (float e = 0f; e < time; e += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, e / time);
                SetAlpha(Mathf.Lerp(from, to, k));
                step?.Invoke(k);
                yield return null;
            }
            SetAlpha(to);
            step?.Invoke(1f);
        }

        void SetAlpha(float a)
        {
            var c = domeMat.color;
            c.a = a;
            domeMat.color = c;
        }

        void LateUpdate()
        {
            if (target == null) return;
            var cam = Camera.main;
            if (cam != null)
            {
                // El domo rodea la cabeza y llega justo detrás de la pieza.
                float dist = Vector3.Distance(cam.transform.position, target.transform.position);
                dome.position = cam.transform.position;
                dome.localScale = Vector3.one * (dist + 2.2f) * 2f;
            }
            if (!ending)
            {
                target.transform.Rotate(0f, SpinSpeed * Time.deltaTime, 0f, Space.World);
                if (Time.time - started > Duration) End();
            }
        }
    }
}
