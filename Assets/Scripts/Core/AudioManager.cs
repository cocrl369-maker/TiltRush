using System.Collections.Generic;
using UnityEngine;

namespace TiltRush
{
    /// <summary>
    /// Music: drop .mp3/.ogg/.wav files (e.g. from Newgrounds) into Assets/Resources/Music - they are
    /// picked up automatically and played in random order.
    /// SFX: drop clips into Assets/Resources/Sfx named click, pickup, launch, die. Missing ones are generated.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        AudioSource music, sfx;
        AudioClip[] tracks;
        int current = -1;
        readonly Dictionary<string, AudioClip> sfxCache = new Dictionary<string, AudioClip>();

        public string CurrentTrackName => (current >= 0 && tracks != null && current < tracks.Length) ? tracks[current].name : "-";

        void Awake()
        {
            tracks = Resources.LoadAll<AudioClip>("Music");
            music = gameObject.AddComponent<AudioSource>();
            sfx = gameObject.AddComponent<AudioSource>();
            ApplyVolumes();
        }

        void Update()
        {
            if (tracks.Length > 0 && !music.isPlaying && Application.isFocused) Next();
        }

        public void Next()
        {
            if (tracks.Length == 0) return;
            int n = current;
            if (tracks.Length > 1) while (n == current) n = Random.Range(0, tracks.Length);
            else n = 0;
            current = n;
            music.clip = tracks[current];
            music.Play();
        }

        public void ApplyVolumes()
        {
            music.volume = SaveManager.Data.musicVolume;
            sfx.volume = SaveManager.Data.sfxVolume;
        }

        public void Play(string name)
        {
            if (!sfxCache.TryGetValue(name, out var clip))
            {
                clip = Resources.Load<AudioClip>("Sfx/" + name) ?? Generate(name);
                sfxCache[name] = clip;
            }
            if (clip != null) sfx.PlayOneShot(clip);
        }

        static AudioClip Generate(string name)
        {
            switch (name)
            {
                case "click":  return Chime(name, 0.07f, 659f, 988f);   // bright two-note "ding"
                case "press":  return Tone(name, 240, 110, 0.07f);        // soft thump when pressing a button
                case "warn":   return Tone(name, 800, 800, 0.15f);
                case "pickup": return Tone(name, 500, 1200, 0.2f);
                case "launch": return Tone(name, 300, 900, 0.25f);
                case "die":    return Tone(name, 400, 60, 0.5f);
                default:       return null;
            }
        }

        /// <summary>Little bell melody: each note is a decaying sine with two overtones.</summary>
        static AudioClip Chime(string name, float step, params float[] notes)
        {
            const int rate = 44100;
            int n = (int)(rate * (step * (notes.Length - 1) + 0.35f));
            var data = new float[n];
            for (int k = 0; k < notes.Length; k++)
            {
                int start = (int)(k * step * rate);
                for (int i = start; i < n; i++)
                {
                    float t = (i - start) / (float)rate;
                    float env = Mathf.Exp(-t * 12f);
                    if (env < 0.002f) break;
                    float f = notes[k];
                    data[i] += (Mathf.Sin(2f * Mathf.PI * f * t) + 0.4f * Mathf.Sin(4f * Mathf.PI * f * t) + 0.2f * Mathf.Sin(6f * Mathf.PI * f * t)) * env * 0.22f;
                }
            }
            for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(data[i], -1f, 1f);
            var clip = AudioClip.Create(name, n, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static AudioClip Tone(string name, float f0, float f1, float dur)
        {
            const int rate = 44100;
            int n = (int)(rate * dur);
            var data = new float[n];
            float phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)n;
                phase += 2f * Mathf.PI * Mathf.Lerp(f0, f1, t) / rate;
                data[i] = Mathf.Sin(phase) * (1f - t) * 0.4f;
            }
            var clip = AudioClip.Create(name, n, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
