using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace MuseoAutista
{
    /// <summary>
    /// Reproductor de música persistente entre escenas.
    /// Lista = canciones incluidas en la app + canciones del celular (carpetas Music y Download).
    /// </summary>
    public class MusicPlayer : MonoBehaviour
    {
        public List<AudioClip> bundled = new List<AudioClip>();
        [Tooltip("Portadas de las canciones incluidas (mismo orden que 'bundled').")]
        public List<Texture2D> bundledCovers = new List<Texture2D>();
        [Range(0f, 1f)] public float volume = 0.35f;
        public bool autoplay = true;

        public static MusicPlayer Instance { get; private set; }

        class Track
        {
            public string title;
            public AudioClip clip;
            public string path;
            public Texture2D cover;
            public bool coverLoaded;
        }

        readonly List<Track> tracks = new List<Track>();
        AudioSource source;
        int index;
        bool wantPlaying;
        bool loading;
        AudioClip externalClip;
        bool ducked;

        public bool IsPlaying => wantPlaying;
        public int Count => tracks.Count;
        public string Status { get; private set; } = "";

        /// <summary>Portada de la canción actual (null si no tiene).</summary>
        public Texture2D CurrentCover
        {
            get
            {
                if (tracks.Count == 0) return null;
                var t = tracks[index];
                if (!t.coverLoaded && t.path != null)
                {
                    t.coverLoaded = true;
                    var bytes = Id3Cover.FromFile(t.path);
                    if (bytes != null)
                    {
                        var tex = new Texture2D(2, 2);
                        if (tex.LoadImage(bytes)) t.cover = tex; else Destroy(tex);
                    }
                }
                return t.cover;
            }
        }

        /// <summary>Baja la música (por ejemplo durante el modo Contemplar).</summary>
        public void SetDuck(bool on) { ducked = on; }

        public string CurrentTitle
        {
            get
            {
                if (tracks.Count == 0) return "Sin canciones";
                return (loading ? "Cargando... " : "") + tracks[index].title;
            }
        }

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            source = gameObject.AddComponent<AudioSource>();
            source.spatialBlend = 0f;
            source.volume = volume;
            source.priority = 0;
            for (int i = 0; i < bundled.Count; i++)
                if (bundled[i] != null)
                    tracks.Add(new Track
                    {
                        title = bundled[i].name, clip = bundled[i], coverLoaded = true,
                        cover = i < bundledCovers.Count ? bundledCovers[i] : null
                    });
            if (autoplay && tracks.Count > 0) Play();
        }

        void Update()
        {
            source.volume = Mathf.MoveTowards(source.volume, ducked ? volume * 0.15f : volume, Time.deltaTime * 0.8f);
            // Pasa a la siguiente canción cuando termina la actual.
            if (wantPlaying && !loading && !source.isPlaying && Application.isFocused && source.clip != null
                && source.time == 0f)
                Next();
        }

        public void TogglePlay()
        {
            if (wantPlaying) Pause(); else Play();
        }

        public void Play()
        {
            if (tracks.Count == 0) return;
            wantPlaying = true;
            if (source.clip != null && source.time > 0f) { source.UnPause(); return; }
            StartCoroutine(PlayIndex(index));
        }

        public void Pause()
        {
            wantPlaying = false;
            source.Pause();
        }

        public void Next()
        {
            if (tracks.Count == 0) return;
            index = (index + 1) % tracks.Count;
            wantPlaying = true;
            StartCoroutine(PlayIndex(index));
        }

        public void Prev()
        {
            if (tracks.Count == 0) return;
            index = (index - 1 + tracks.Count) % tracks.Count;
            wantPlaying = true;
            StartCoroutine(PlayIndex(index));
        }

        IEnumerator PlayIndex(int i)
        {
            var t = tracks[i];
            source.Stop();
            source.time = 0f;
            if (t.clip == null && t.path != null)
            {
                loading = true;
                using (var req = UnityWebRequestMultimedia.GetAudioClip("file://" + t.path, AudioTypeFor(t.path)))
                {
                    ((DownloadHandlerAudioClip)req.downloadHandler).streamAudio = true;
                    yield return req.SendWebRequest();
                    loading = false;
                    if (req.result != UnityWebRequest.Result.Success || i != index) yield break;
                    if (externalClip != null) Destroy(externalClip);
                    externalClip = DownloadHandlerAudioClip.GetContent(req);
                    externalClip.name = t.title;
                    source.clip = externalClip;
                }
            }
            else source.clip = t.clip;
            if (wantPlaying) source.Play();
        }

        static AudioType AudioTypeFor(string path)
        {
            string ext = Path.GetExtension(path).ToLowerInvariant();
            return ext == ".ogg" ? AudioType.OGGVORBIS : ext == ".wav" ? AudioType.WAV : AudioType.MPEG;
        }

        /// <summary>Pide permiso de audio y agrega las canciones de las carpetas Music y Download del celular.</summary>
        public void ScanPhone()
        {
            StartCoroutine(ScanRoutine());
        }

        IEnumerator ScanRoutine()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            string perm = AndroidSdk() >= 33 ? "android.permission.READ_MEDIA_AUDIO" : "android.permission.READ_EXTERNAL_STORAGE";
            if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(perm))
            {
                Status = "Acepta el permiso en la pantalla";
                UnityEngine.Android.Permission.RequestUserPermission(perm);
                float t = 0f;
                while (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(perm) && t < 20f)
                {
                    t += Time.unscaledDeltaTime;
                    yield return null;
                }
                if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(perm)) { Status = "Permiso denegado"; yield break; }
            }
            string[] dirs = { "/storage/emulated/0/Music", "/storage/emulated/0/Download" };
#else
            string[] dirs = { System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyMusic) };
            yield return null;
#endif
            int added = 0;
            foreach (var d in dirs)
            {
                if (!Directory.Exists(d)) continue;
                string[] files;
                try { files = Directory.GetFiles(d, "*.*", SearchOption.AllDirectories); }
                catch { continue; }
                foreach (var f in files)
                {
                    string ext = Path.GetExtension(f).ToLowerInvariant();
                    if (ext != ".mp3" && ext != ".ogg" && ext != ".wav") continue;
                    if (tracks.Exists(x => x.path == f)) continue;
                    tracks.Add(new Track { title = Path.GetFileNameWithoutExtension(f), path = f });
                    added++;
                }
            }
            Status = added > 0 ? "+" + added + " canciones del celular" : "No hay canciones en Música o Descargas";
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        static int AndroidSdk()
        {
            using (var v = new AndroidJavaClass("android.os.Build$VERSION")) return v.GetStatic<int>("SDK_INT");
        }
#endif
    }
}
