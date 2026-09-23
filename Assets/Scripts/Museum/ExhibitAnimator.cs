using System;
using System.Collections;
using UnityEngine;

namespace MuseoAutista
{
    public enum ExhibitKind { Jet, Dinosaur, Fossil, Car, Mushroom }

    /// <summary>
    /// Modelo de exhibición que se anima al mirarlo:
    /// aviones encienden motores y despegan un poco, dinosaurios rugen (animación real
    /// del modelo si tiene Animator), fósiles se desarman y vuelven a armarse, carros
    /// arrancan con luces y ruedas, y los hongos rebotan soltando esporas.
    /// </summary>
    public class ExhibitAnimator : GazeInteractable
    {
        public ExhibitKind kind;
        public Transform body;

        [Header("Sonido de la acción (opcional)")]
        public AudioClip actionClip;
        [Range(0f, 1f)] public float actionVolume = 0.8f;
        public float actionPitch = 1f;

        [Header("Avión / Carro")]
        public GameObject[] glowParts;
        public Transform[] wheels;
        public Transform turntable;

        [Header("Dinosaurio")]
        public Animator animator;
        public Transform head;
        public Transform jaw;
        public Transform tail;

        [Header("Fósil")]
        public Transform[] bones;

        [Header("Hongo")]
        public ParticleSystem spores;

        public static event Action<ExhibitAnimator> AnyPlayed;

        bool playing;
        AudioSource audioSource;
        Vector3 bodyPos;
        Quaternion bodyRot;
        Vector3 bodyScale;
        Quaternion headRot, jawRot, tailRot;
        Vector3[] boneHome;
        float turntableSpeed = 8f;
        float phase;

        void Awake()
        {
            if (body == null) body = transform;
            bodyPos = body.localPosition;
            bodyRot = body.localRotation;
            bodyScale = body.localScale;
            if (head) headRot = head.localRotation;
            if (jaw) jawRot = jaw.localRotation;
            if (tail) tailRot = tail.localRotation;
            if (bones != null)
            {
                boneHome = new Vector3[bones.Length];
                for (int i = 0; i < bones.Length; i++) boneHome[i] = bones[i].localPosition;
            }
            if (actionClip != null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0.7f;
                audioSource.minDistance = 3f;
                audioSource.maxDistance = 25f;
                audioSource.pitch = actionPitch;
            }
            SetGlow(false);
            phase = UnityEngine.Random.value * 10f;
        }

        public override bool IsInteractable => base.IsInteractable && !playing;

        void Update()
        {
            float t = Time.time + phase;
            if (kind == ExhibitKind.Dinosaur && !playing && animator == null)
            {
                if (tail) tail.localRotation = tailRot * Quaternion.Euler(0f, Mathf.Sin(t * 1.1f) * 7f, 0f);
                body.localScale = Vector3.Scale(bodyScale, new Vector3(1f, 1f + Mathf.Sin(t * 1.6f) * 0.012f, 1f));
            }
            if (kind == ExhibitKind.Mushroom && !playing)
                body.localScale = Vector3.Scale(bodyScale, new Vector3(1f, 1f + Mathf.Sin(t * 1.2f) * 0.015f, 1f));
            if (turntable) turntable.Rotate(0f, turntableSpeed * Time.deltaTime, 0f, Space.Self);
        }

        public override void OnGazeActivate()
        {
            if (playing) return;
            AnyPlayed?.Invoke(this);
            switch (kind)
            {
                case ExhibitKind.Jet: StartCoroutine(PlayJet()); break;
                case ExhibitKind.Dinosaur: StartCoroutine(PlayDino()); break;
                case ExhibitKind.Fossil: StartCoroutine(PlayFossil()); break;
                case ExhibitKind.Car: StartCoroutine(PlayCar()); break;
                case ExhibitKind.Mushroom: StartCoroutine(PlayMushroom()); break;
            }
        }

        /// <summary>Reproduce el sonido propio de la exhibición o, si no tiene, el del banco de sonidos.</summary>
        void PlaySound(string fallback, float fallbackVolume)
        {
            if (audioSource != null)
            {
                audioSource.volume = actionVolume;
                audioSource.clip = actionClip;
                audioSource.Play();
            }
            else Sfx.Play(fallback, fallbackVolume);
        }

        IEnumerator StopSound(float fade = 0.6f)
        {
            if (audioSource == null || !audioSource.isPlaying) yield break;
            float v0 = audioSource.volume;
            for (float t = 0f; t < fade; t += Time.deltaTime)
            {
                audioSource.volume = Mathf.Lerp(v0, 0f, t / fade);
                yield return null;
            }
            audioSource.Stop();
        }

        IEnumerator PlayJet()
        {
            playing = true;
            PlaySound("engine", 0.45f);
            SetGlow(true);
            yield return Animate(2.8f, e =>
            {
                float lift = Mathf.Sin(e * Mathf.PI);
                body.localPosition = bodyPos + Vector3.up * 0.35f * lift;
                body.localRotation = bodyRot * Quaternion.Euler(-9f * lift, 0f, Mathf.Sin(e * Mathf.PI * 2f) * 4f);
                foreach (var g in glowParts)
                    if (g) g.transform.localScale = new Vector3(1f, 1f, 0.8f + 0.4f * Mathf.PerlinNoise(Time.time * 20f, 0f));
            });
            SetGlow(false);
            body.localPosition = bodyPos;
            body.localRotation = bodyRot;
            yield return StopSound();
            playing = false;
        }

        IEnumerator PlayDino()
        {
            playing = true;
            PlaySound("roar", 0.55f);
            if (animator != null)
            {
                animator.SetTrigger("Rugir");
                yield return new WaitForSeconds(3.2f);
            }
            else
            {
                yield return Animate(2.0f, e =>
                {
                    float k = Mathf.Sin(e * Mathf.PI);
                    if (head) head.localRotation = headRot * Quaternion.Euler(-22f * k, Mathf.Sin(e * 12f) * 3f * k, 0f);
                    if (!head) body.localPosition = bodyPos + Vector3.up * Mathf.Abs(Mathf.Sin(e * Mathf.PI * 3f)) * 0.12f * k;
                    if (jaw) jaw.localRotation = jawRot * Quaternion.Euler(28f * k, 0f, 0f);
                    if (tail) tail.localRotation = tailRot * Quaternion.Euler(0f, Mathf.Sin(e * 18f) * 14f * k, 0f);
                });
                if (head) head.localRotation = headRot;
                if (jaw) jaw.localRotation = jawRot;
                body.localPosition = bodyPos;
            }
            playing = false;
        }

        IEnumerator PlayFossil()
        {
            playing = true;
            PlaySound("bones", 0.5f);
            if (bones != null && bones.Length > 0)
            {
                Vector3 center = Vector3.zero;
                foreach (var h in boneHome) center += h;
                center /= boneHome.Length;
                yield return Animate(2.6f, e =>
                {
                    float k = e < 0.35f ? Mathf.SmoothStep(0f, 1f, e / 0.35f)
                            : e > 0.65f ? Mathf.SmoothStep(1f, 0f, (e - 0.65f) / 0.35f) : 1f;
                    for (int i = 0; i < bones.Length; i++)
                    {
                        Vector3 dir = boneHome[i] - center;
                        if (dir.sqrMagnitude < 0.0001f) dir = Vector3.up;
                        bones[i].localPosition = boneHome[i] + (dir.normalized * 0.25f + dir * 0.35f + Vector3.up * 0.1f) * k;
                    }
                });
                for (int i = 0; i < bones.Length; i++) bones[i].localPosition = boneHome[i];
            }
            else
            {
                yield return Animate(2f, e => body.localRotation = bodyRot * Quaternion.Euler(0f, 360f * Mathf.SmoothStep(0f, 1f, e), 0f));
                body.localRotation = bodyRot;
            }
            playing = false;
        }

        IEnumerator PlayCar()
        {
            playing = true;
            PlaySound("car", 0.5f);
            SetGlow(true);
            turntableSpeed = 30f;
            yield return Animate(3.5f, e =>
            {
                foreach (var w in wheels) if (w) w.Rotate(Vector3.right, 720f * Time.deltaTime, Space.Self);
                body.localPosition = bodyPos + Vector3.up * Mathf.Abs(Mathf.Sin(e * 40f)) * 0.008f;
            });
            body.localPosition = bodyPos;
            turntableSpeed = 8f;
            yield return StopSound(1f);
            SetGlow(false);
            playing = false;
        }

        IEnumerator PlayMushroom()
        {
            playing = true;
            PlaySound("click", 0.4f);
            if (spores != null) spores.Play();
            yield return Animate(1.8f, e =>
            {
                // Rebotes "blanditos" (aplastar y estirar) que se van calmando.
                float damp = 1f - e;
                float s = Mathf.Sin(e * Mathf.PI * 6f) * 0.18f * damp;
                body.localScale = Vector3.Scale(bodyScale, new Vector3(1f + s * 0.6f, 1f - s, 1f + s * 0.6f));
            });
            body.localScale = bodyScale;
            playing = false;
        }

        static IEnumerator Animate(float duration, Action<float> step)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                step(Mathf.Clamp01(t / duration));
                yield return null;
            }
            step(1f);
        }

        void SetGlow(bool on)
        {
            if (glowParts == null) return;
            foreach (var g in glowParts) if (g) g.SetActive(on);
        }
    }
}
