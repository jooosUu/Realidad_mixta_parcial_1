using UnityEngine;

namespace MuseoAutista
{
    /// <summary>
    /// Música de fondo en bucle, a volumen bajo. Sobrevive a los cambios de escena
    /// (no se reinicia) mientras la canción sea la misma.
    /// </summary>
    public class MusicPlayer : MonoBehaviour
    {
        public AudioClip music;
        [Range(0f, 1f)] public float volume = 0.22f;

        static MusicPlayer current;

        void Awake()
        {
            if (music == null) { Destroy(gameObject); return; }
            if (current != null)
            {
                if (current.music == music) { Destroy(gameObject); return; }
                Destroy(current.gameObject);
            }
            current = this;
            DontDestroyOnLoad(gameObject);
            var src = gameObject.AddComponent<AudioSource>();
            src.clip = music;
            src.loop = true;
            src.volume = volume;
            src.spatialBlend = 0f;
            src.priority = 0;
            src.Play();
        }
    }
}
