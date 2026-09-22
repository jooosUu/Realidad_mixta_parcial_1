using System;
using System.Collections.Generic;
using UnityEngine;

namespace MuseoAutista
{
    /// <summary>
    /// Efectos de sonido sintetizados en tiempo de ejecución (sin archivos de audio).
    /// Volúmenes suaves para no saturar al usuario.
    /// </summary>
    public static class Sfx
    {
        const int Rate = 22050;
        static AudioSource source;
        static Dictionary<string, AudioClip> clips;

        static SoundBank bank;
        static bool bankLoaded;

        public static void Play(string id, float volume = 0.5f)
        {
            Ensure();
            if (!bankLoaded) { bank = Resources.Load<SoundBank>("SoundBank"); bankLoaded = true; }
            var entry = bank != null ? bank.Get(id) : null;
            if (entry != null)
            {
                source.pitch = entry.pitch;
                source.PlayOneShot(entry.clip, entry.volume);
                return;
            }
            source.pitch = 1f;
            if (clips.TryGetValue(id, out var clip)) source.PlayOneShot(clip, volume);
        }

        static void Ensure()
        {
            if (source != null) return;
            var go = new GameObject("Sfx");
            UnityEngine.Object.DontDestroyOnLoad(go);
            source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            if (clips == null) Build();
        }

        static void Build()
        {
            var rnd = new System.Random(7);
            float Noise() => (float)(rnd.NextDouble() * 2.0 - 1.0);

            clips = new Dictionary<string, AudioClip>
            {
                ["click"] = Make("click", 0.07f, t => Sine(1100f, t) * Decay(t, 40f)),
                ["teleport"] = Make("teleport", 0.3f, t => Sine(300f + 1600f * t, t) * 0.6f * Env(t, 0.3f, 0.02f)),
                ["grab"] = Make("grab", 0.22f, t => Sine(t < 0.1f ? 620f : 930f, t) * 0.7f * Env(t, 0.22f, 0.01f)),
                ["place"] = Make("place", 0.8f, t => (Sine(880f, t) + 0.6f * Sine(1320f, t)) * 0.5f * Decay(t, 5f)),
                ["error"] = Make("error", 0.3f, t => Mathf.Sign(Sine(170f, t)) * 0.25f * Env(t, 0.3f, 0.01f)),
                ["success"] = Make("success", 0.9f, t =>
                {
                    float[] notes = { 523.25f, 659.25f, 783.99f, 1046.5f };
                    int i = Mathf.Min(3, (int)(t / 0.15f));
                    float lt = t - i * 0.15f;
                    return Sine(notes[i], t) * 0.5f * Decay(lt, i == 3 ? 3f : 10f);
                }),
                ["roar"] = MakeFiltered("roar", 1.5f, Noise, 0.04f, t =>
                    (0.75f + 0.25f * Mathf.Sin(2f * Mathf.PI * 13f * t)) * Env(t, 1.5f, 0.12f), t => Sine(70f - 20f * t, t) * 0.5f),
                ["engine"] = MakeFiltered("engine", 2.0f, Noise, 0.08f, t => Env(t, 2.0f, 0.4f),
                    t => Saw(90f + 40f * t, t) * 0.25f),
                ["car"] = MakeFiltered("car", 1.6f, Noise, 0.05f, t => Env(t, 1.6f, 0.2f) * 0.6f,
                    t => (t < 0.45f ? (Sine(392f, t) + Sine(494f, t)) * 0.3f * Env(t, 0.45f, 0.02f) : 0f) + Saw(60f, t) * 0.2f),
                ["bones"] = MakeFiltered("bones", 0.7f, Noise, 0.6f,
                    t => Decay(t % 0.12f, 60f) * (t < 0.6f ? 1f : 0f), t => 0f),
            };
        }

        static float Sine(float f, float t) => Mathf.Sin(2f * Mathf.PI * f * t);
        static float Saw(float f, float t) => 2f * (f * t - Mathf.Floor(f * t + 0.5f));
        static float Decay(float t, float k) => Mathf.Exp(-k * t);

        static float Env(float t, float duration, float attack)
        {
            float a = Mathf.Clamp01(t / attack);
            float r = Mathf.Clamp01((duration - t) / (duration * 0.4f));
            return a * r;
        }

        static AudioClip Make(string name, float duration, Func<float, float> wave)
        {
            int n = (int)(Rate * duration);
            var data = new float[n];
            for (int i = 0; i < n; i++) data[i] = wave(i / (float)Rate) * 0.8f;
            var clip = AudioClip.Create(name, n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>Ruido con filtro paso bajo + tono base (rugidos, motores).</summary>
        static AudioClip MakeFiltered(string name, float duration, Func<float> noise, float alpha,
            Func<float, float> envelope, Func<float, float> tone)
        {
            int n = (int)(Rate * duration);
            var data = new float[n];
            float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                lp += alpha * (noise() - lp);
                data[i] = Mathf.Clamp((lp * 2.5f + tone(t)) * envelope(t), -1f, 1f) * 0.8f;
            }
            var clip = AudioClip.Create(name, n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
