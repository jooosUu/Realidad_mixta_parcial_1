using System;
using UnityEngine;

namespace MuseoAutista
{
    /// <summary>
    /// Sonidos de la interfaz (se carga desde Resources/SoundBank). Si un id no tiene
    /// clip asignado, Sfx usa el sonido sintetizado de respaldo.
    /// </summary>
    [CreateAssetMenu(menuName = "Museo Autista/Sound Bank")]
    public class SoundBank : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public string id;
            public AudioClip clip;
            [Range(0f, 1f)] public float volume = 0.7f;
            public float pitch = 1f;
        }

        public Entry[] entries = new Entry[0];

        public Entry Get(string id) => Array.Find(entries, e => e.id == id && e.clip != null);
    }
}
