using System.Collections.Generic;
using UnityEngine;

namespace TiltRush
{
    /// <summary>
    /// MUSIC - put .mp3 / .ogg / .wav files (e.g. from Newgrounds) into sub-folders of Assets/Resources/Music:
    ///   Music/Menu       menus and lobby
    ///   Music/Game       all game modes (fallback for the folders below)
    ///   Music/Campaign, Music/Classic, Music/Hard, Music/Dodge, Music/Asteroids, Music/Coop, Music/Online, Music/Tutorial
    ///                    optional: own music for one mode
    /// A missing folder borrows a related mode (Tutorial -> Campaign, Hard/Coop/Online -> Classic), then Music/Game,
    /// Music/Campaign, Music/Menu. Tracks play in random order and
    /// cross-fade when you go from the menu into a game. Name the artists in Assets/Resources/credits.txt
    /// (shown by the CREDITS button in the main menu).
    /// SFX: drop clips into Assets/Resources/Sfx named click, pickup, launch, die ... Missing ones are generated.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        AudioSource music, sfx;
        AudioClip[] tracks = new AudioClip[0];
        int current = -1;
        string context = "";
        float fade = 1f, fadeTarget = 1f;
        AudioClip[] pending;
        readonly Dictionary<string, AudioClip> sfxCache = new Dictionary<string, AudioClip>();
        readonly Dictionary<string, AudioClip[]> playlists = new Dictionary<string, AudioClip[]>();

        // Beat detection for visuals: 0..1, jumps up on loud moments of the music and decays quickly.
        public float Beat { get; private set; }
        public bool HasMusic { get; private set; }
        readonly float[] samples = new float[256];
        float avgLevel = 0.05f;

        public string CurrentTrackName => (current >= 0 && tracks != null && current < tracks.Length) ? tracks[current].name : "-";

        void Awake()
        {
            music = gameObject.AddComponent<AudioSource>();
            sfx = gameObject.AddComponent<AudioSource>();
            ApplyVolumes();
            SetContext("Menu");
        }

        AudioClip[] Load(string folder)
        {
            if (!playlists.TryGetValue(folder, out var list))
            {
                list = Resources.LoadAll<AudioClip>(folder);
                playlists[folder] = list;
            }
            return list;
        }

        /// <summary>Modes without their own music folder use the music of this mode.</summary>
        static readonly Dictionary<string, string> Borrow = new Dictionary<string, string>
        {
            { "Tutorial", "Campaign" },
            { "Hard", "Classic" },
            { "Coop", "Classic" },
            { "Online", "Classic" },
        };

        /// <summary>"Menu", "Game" or a mode name ("Asteroids", "Campaign" ...). Switches the playlist with a cross-fade.</summary>
        public void SetContext(string ctx)
        {
            if (ctx == context) return;
            context = ctx;
            AudioClip[] list = Load("Music/" + ctx);
            // a mode without its own folder borrows the music of a related mode (never a random track of another mode)
            string borrow;
            if (list.Length == 0 && Borrow.TryGetValue(ctx, out borrow)) list = Load("Music/" + borrow);
            if (list.Length == 0 && ctx != "Menu") list = Load("Music/Game");
            if (list.Length == 0 && ctx != "Menu") list = Load("Music/Campaign");
            if (list.Length == 0) list = Load("Music/Menu");
            if (list.Length == 0) list = Load("Music");          // last resort: loose files directly in Music/
            if (list == tracks && music.isPlaying) { pending = null; fadeTarget = 1f; return; }   // same music: keep playing
            pending = list;
            if (music.isPlaying) fadeTarget = 0f;                 // fade out, then switch (see Update)
            else { tracks = pending; pending = null; Next(); }
        }

        void Update()
        {
            // cross-fade between playlists
            fade = Mathf.MoveTowards(fade, fadeTarget, Time.unscaledDeltaTime / 0.8f);
            if (pending != null && fade <= 0f) { tracks = pending; pending = null; Next(); fadeTarget = 1f; }
            ApplyVolumes();

            if (pending == null && tracks.Length > 0 && !music.isPlaying && Application.isFocused) Next();
            UpdateBeat();
        }

        void UpdateBeat()
        {
            float dt = Time.unscaledDeltaTime;
            if (!music.isPlaying || music.volume <= 0.01f) { HasMusic = false; Beat = Mathf.MoveTowards(Beat, 0f, dt * 3f); return; }

            music.GetOutputData(samples, 0);
            float sum = 0f;
            for (int i = 0; i < samples.Length; i++) sum += samples[i] * samples[i];
            float rms = Mathf.Sqrt(sum / samples.Length);

            avgLevel = Mathf.Lerp(avgLevel, rms, 1f - Mathf.Exp(-0.8f * dt));     // slow average = "normal" loudness
            HasMusic = avgLevel > 0.0005f;
            float target = Mathf.Clamp01((rms / Mathf.Max(0.0005f, avgLevel) - 0.85f) * 1.6f);
            Beat = target > Beat ? Mathf.Lerp(Beat, target, 0.6f) : Mathf.MoveTowards(Beat, target, dt * 2.5f);
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
            fadeTarget = 1f;
        }

        public void ApplyVolumes()
        {
            if (music == null) return;
            music.volume = SaveManager.Data.musicVolume * fade;
            sfx.volume = SaveManager.Data.sfxVolume;
        }

        public void Play(string name) { Play(name, 1f); }

        public void Play(string name, float volume)
        {
            if (!sfxCache.TryGetValue(name, out var clip))
            {
                clip = Resources.Load<AudioClip>("Sfx/" + name) ?? Generate(name);
                sfxCache[name] = clip;
            }
            if (clip != null) sfx.PlayOneShot(clip, volume);
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
                case "die":    return BrickBreak(name);                     // plastic bricks scattering
                case "dieold": return Tone(name, 400, 60, 0.5f);
                case "shoot":  return Tone(name, 1100, 500, 0.06f);
                case "crack":  return Tone(name, 220, 90, 0.12f);
                default:       return null;
            }
        }

        /// <summary>
        /// Our own "toy bricks fall apart" sound (generated, so no licence problems): a short dull knock, then many
        /// small hard plastic clicks that scatter and get quieter - like a brick model hitting the floor.
        /// </summary>
        static AudioClip BrickBreak(string name)
        {
            const int rate = 44100;
            int n = (int)(rate * 0.75f);
            var data = new float[n];
            var rnd = new System.Random(1234);   // same sound every time

            // dull knock at the start
            for (int i = 0; i < (int)(rate * 0.09f); i++)
            {
                float t = i / (float)rate;
                data[i] += Mathf.Sin(2f * Mathf.PI * 170f * t) * Mathf.Exp(-t * 45f) * 0.45f;
            }

            // 26 clicks: most of them early, a few late ones (pieces still bouncing)
            for (int k = 0; k < 26; k++)
            {
                float u = (float)rnd.NextDouble();
                float start = 0.01f + 0.55f * u * u;
                float freq = 1800f + (float)rnd.NextDouble() * 3200f;     // hard plastic = high, short resonances
                float amp = (0.35f + 0.4f * (float)rnd.NextDouble()) * (1f - 0.6f * u);
                float decay = 70f + (float)rnd.NextDouble() * 80f;
                int s0 = (int)(start * rate);
                for (int i = s0; i < n && i < s0 + rate / 20; i++)
                {
                    float t = (i - s0) / (float)rate;
                    float env = Mathf.Exp(-t * decay);
                    float noise = ((float)rnd.NextDouble() * 2f - 1f) * Mathf.Exp(-t * 400f);   // the "tick" of the impact
                    data[i] += (Mathf.Sin(2f * Mathf.PI * freq * t) * 0.7f + Mathf.Sin(2f * Mathf.PI * freq * 1.58f * t) * 0.3f) * env * amp * 0.5f + noise * amp * 0.35f;
                }
            }
            for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(data[i], -1f, 1f);
            var clip = AudioClip.Create(name, n, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
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
