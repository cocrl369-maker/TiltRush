using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TiltRush
{
    /// <summary>
    /// Settings with tabs: GRAPHICS, SOUND, INTERFACE, CONTROLS, PROFILE. Each row is label (left) + control (right).
    /// Everything is saved in SaveData and takes effect immediately. Add a setting: a field in SaveData +
    /// one line here (Choice / Toggle / SliderRow) + use the value where it matters.
    /// </summary>
    public class OptionsScreen : UIScreen
    {
        static readonly string[] TabNames = { "GRAPHICS", "SOUND", "INTERFACE", "CONTROLS", "PROFILE" };
        static readonly Color[] TabColors = { Neon.Blue, Neon.Pink, Neon.Orange, Neon.Green, Neon.Purple };

        RectTransform[] pages;
        Button[] tabs;
        int tab;
        readonly List<System.Action> refreshers = new List<System.Action>();
        Text trackText, statsText, skinsText, onlineText;
        InputField nameField;
        Button resetBtn;
        protected override bool HasVignette { get { return true; } }
        /// <summary>Opened from the pause menu: keep the game music.</summary>
        public override string MusicContext { get { return GameWorld.Current != null ? null : "Menu"; } }
        static bool InGame { get { return GameWorld.Current != null; } }

        const float ColW = 760f, ColX = 420f, Top = 300f;

        protected override void Build()
        {
            UIKit.Title(transform, "OPTIONS", 96).rectTransform.Also(rt => UIKit.Anchor(rt, new Vector2(0.5f, 1f), new Vector2(0, -25), new Vector2(1200, 120)));

            // tab row
            tabs = new Button[TabNames.Length];
            const float tw = 300f, tg = 18f;
            for (int i = 0; i < tabs.Length; i++)
            {
                int idx = i;
                tabs[i] = UIKit.MakeButton(transform, TabNames[i], TabColors[i], new Vector2(tw, 88), () => SetTab(idx), 34);
                UIKit.Anchor((RectTransform)tabs[i].transform, new Vector2(0.5f, 1f),
                             new Vector2((i - (tabs.Length - 1) / 2f) * (tw + tg), -160), new Vector2(tw, 88));
            }

            UIKit.Panel(transform, 0, 270, 1720, 650);

            pages = new RectTransform[TabNames.Length];
            for (int i = 0; i < pages.Length; i++) pages[i] = UIKit.Stretch(UIKit.Rect("Page" + TabNames[i], transform));

            BuildGraphics(pages[0]);
            BuildSound(pages[1]);
            BuildInterface(pages[2]);
            BuildControls(pages[3]);
            BuildProfile(pages[4]);

            MenuUtil.AddBack(transform, () =>
            {
                if (InGame) App.I.BackToPause();               // came from the pause menu
                else { SaveManager.Save(); UI.Show<MainMenuScreen>(); }
            });
        }

        UIStack Column(RectTransform page, bool right)
        {
            var st = new UIStack(page, Top, right ? ColX : -ColX, ColW) { Spacing = 16f };
            st.onChanged = RefreshAll;
            return st;
        }

        // ------------------------------------------------------------------ GRAPHICS
        void BuildGraphics(RectTransform page)
        {
            var d = (System.Func<SaveData>)(() => SaveManager.Data);
            var l = Column(page, false);
            refreshers.Add(l.Choice("QUALITY", new[] { "LOW", "MEDIUM", "HIGH", "CUSTOM" },
                () => d().quality, v => ApplyQuality(v == 3 ? 0 : v)));
            refreshers.Add(l.Choice("PARTICLES", new[] { "OFF", "FEW", "MANY" }, () => d().particles, v => { d().particles = v; Custom(); }));
            refreshers.Add(l.Toggle("LIGHT TRAIL", () => d().trail, v => { d().trail = v; Custom(); }));
            refreshers.Add(l.Choice("FRAME RATE", new[] { "30 FPS", "60 FPS", "120 FPS" }, () => d().frameRate,
                v => { d().frameRate = v; App.I.ApplyFrameRate(); Custom(); }));

            var r = Column(page, true);
            refreshers.Add(r.Choice("BACKGROUND", new[] { "STILL", "CALM", "FULL" }, () => d().bgMotion, v => { d().bgMotion = v; Custom(); }));
            refreshers.Add(r.SliderRow("BRIGHTNESS", 0.5f, 1.2f, () => d().bgBrightness, v => d().bgBrightness = v));
            refreshers.Add(r.Toggle("SCREEN SHAKE", () => d().screenShake, v => d().screenShake = v));
            r.Label("Weak phone or warm battery? Choose LOW.", 28, Theme.TextDim, 80);
        }

        /// <summary>Presets for weak / normal / strong phones.</summary>
        static void ApplyQuality(int q)
        {
            var d = SaveManager.Data;
            d.quality = q;
            switch (q)
            {
                case 0: d.particles = 1; d.trail = false; d.bgMotion = 1; d.frameRate = 0; break;
                case 1: d.particles = 1; d.trail = true;  d.bgMotion = 2; d.frameRate = 1; break;
                default: d.particles = 2; d.trail = true; d.bgMotion = 2; d.frameRate = 1; break;
            }
            App.I.ApplyFrameRate();
        }

        static void Custom() { SaveManager.Data.quality = 3; }

        // ------------------------------------------------------------------ SOUND
        void BuildSound(RectTransform page)
        {
            var l = Column(page, false);
            refreshers.Add(l.SliderRow("MUSIC", 0f, 1f, () => SaveManager.Data.musicVolume, v => { SaveManager.Data.musicVolume = v; App.I.Audio.ApplyVolumes(); }));
            refreshers.Add(l.SliderRow("EFFECTS", 0f, 1f, () => SaveManager.Data.sfxVolume, v => { SaveManager.Data.sfxVolume = v; App.I.Audio.ApplyVolumes(); }));
            refreshers.Add(l.Toggle("BUTTON SOUNDS", () => SaveManager.Data.buttonSounds, v => SaveManager.Data.buttonSounds = v));
            refreshers.Add(l.Toggle("VIBRATION", () => SaveManager.Data.vibration, v => SaveManager.Data.vibration = v));

            var r = Column(page, true);
            r.Label("MUSIC", 36, Neon.Aqua, 50);
            trackText = r.Label("", 32, Color.white, 90);
            r.Button("NEXT TRACK", Neon.Indigo, 460, 96, () => { App.I.Audio.Next(); RefreshAll(); }, 38);
            r.Label("Put your own music into Assets/Resources/Music.", 26, Theme.TextDim, 70);
        }

        // ------------------------------------------------------------------ INTERFACE
        void BuildInterface(RectTransform page)
        {
            var l = Column(page, false);
            refreshers.Add(l.Choice("HUD SIZE", new[] { "SMALL", "NORMAL", "LARGE" }, () => SaveManager.Data.hudSize, v => SaveManager.Data.hudSize = v));
            refreshers.Add(l.Choice("PAUSE BUTTON", new[] { "RIGHT", "LEFT" }, () => SaveManager.Data.pauseLeft ? 1 : 0, v => SaveManager.Data.pauseLeft = v == 1));
            refreshers.Add(l.Toggle("SHOW FPS", () => SaveManager.Data.showFps, v => SaveManager.Data.showFps = v));

            var r = Column(page, true);
            refreshers.Add(r.Toggle("ANIMATIONS", () => SaveManager.Data.menuAnimations, v => SaveManager.Data.menuAnimations = v));
            r.Label("Left-handed? Put the pause button on the left so your thumb does not cover the arena.", 28, Theme.TextDim, 120, TextAnchor.UpperLeft);
        }

        // ------------------------------------------------------------------ CONTROLS
        void BuildControls(RectTransform page)
        {
            var l = Column(page, false);
            l.Button("TILT SETUP", Neon.Blue, ColW, 96, () => UI.Show<TiltSetupScreen>(), 40);
            refreshers.Add(l.SliderRow("SENSITIVITY", 1f, 6f, () => SaveManager.Data.tiltSensitivity, v => SaveManager.Data.tiltSensitivity = v));
            refreshers.Add(l.Choice("SMOOTHING", new[] { "DIRECT", "NORMAL", "SMOOTH" }, () => SaveManager.Data.tiltSmoothing, v => SaveManager.Data.tiltSmoothing = v));
            refreshers.Add(l.Choice("TILT AXES", new[] { "STANDARD", "SWAPPED" }, () => SaveManager.Data.tiltAxes, v => SaveManager.Data.tiltAxes = v));

            var r = Column(page, true);
            refreshers.Add(r.Toggle("INVERT LEFT/RIGHT", () => SaveManager.Data.invertX, v => SaveManager.Data.invertX = v));
            refreshers.Add(r.Toggle("INVERT UP/DOWN", () => SaveManager.Data.invertY, v => SaveManager.Data.invertY = v));
            r.Label("Tilt right but the ship goes up/down? Switch TILT AXES to SWAPPED. " + Platform.CoopHint, 28, Theme.TextDim, 210, TextAnchor.UpperLeft);
        }

        // ------------------------------------------------------------------ PROFILE
        void BuildProfile(RectTransform page)
        {
            var l = Column(page, false);
            nameField = l.TextField("PLAYER NAME (LEADERBOARD)", SaveManager.Data.playerName, 12, v =>
            {
                v = v.Trim();
                if (v.Length > 0) SaveManager.Data.playerName = v;
            });
            l.Space(10);
            skinsText = l.Label("", 36, Neon.Lime, 60);
            resetBtn = l.Button("RESET PROGRESS", Neon.Red, 520, 96, ConfirmReset, 38);
            l.Space(10);
            onlineText = l.Label("", 28, Theme.TextDim, 70);

            var r = Column(page, true);
            r.Label("STATISTICS", 36, Neon.Aqua, 50);
            statsText = r.Label("", 30, Color.white, 420, TextAnchor.UpperLeft);
        }

        void ConfirmReset()
        {
            UI.ShowDialog("RESET PROGRESS?",
                "Scores, campaign, ELO, statistics and unlocked skins will be deleted.\nYour settings stay.",
                new UIStack.Item("DELETE", Neon.Red, () => { SaveManager.ResetProgress(); UI.Show<OptionsScreen>(); }),
                new UIStack.Item("CANCEL", Neon.Gray, () => UI.Show<OptionsScreen>()));
        }

        // ------------------------------------------------------------------ show / refresh
        void SetTab(int i)
        {
            tab = i;
            for (int k = 0; k < pages.Length; k++)
            {
                pages[k].gameObject.SetActive(k == i);
                UIKit.SetButtonColor(tabs[k], k == i ? TabColors[k] : Theme.Locked, k != i);
            }
            RefreshAll();
        }

        void RefreshAll()
        {
            foreach (var r in refreshers) r();
            trackText.text = "Now playing:\n" + App.I.Audio.CurrentTrackName;

            var d = SaveManager.Data;
            nameField.text = d.playerName;
            skinsText.text = "SKINS: " + Skins.UnlockedCount + " / " + Skins.All.Length;
            onlineText.text = "Online: " + App.I.Online.Status;
            statsText.text =
                "Rounds played:  " + d.runsPlayed.ToString("N0") + "\n" +
                "Time played:  " + Mathf.FloorToInt(d.totalPlaySeconds / 60f) + " min\n" +
                "Dots destroyed:  " + d.totalKills.ToString("N0") + "\n" +
                "Pickups collected:  " + d.totalPickups.ToString("N0") + "\n" +
                "Best Classic:  " + d.bestClassicScore.ToString("N0") + " pts\n" +
                "Best Hard:  " + d.bestHardScore.ToString("N0") + " pts\n" +
                "Best Dodge:  " + d.bestDodge.ToString("0.0") + " s\n" +
                "Campaign:  " + d.campaignDone + " / " + Campaign.Levels.Length + "\n" +
                "Online:  " + d.onlineWins + " W / " + d.onlineLosses + " L  (ELO " + d.elo + ")";
        }

        public override void OnShow()
        {
            resetBtn.gameObject.SetActive(!InGame);           // no resetting in the middle of a round
            SetTab(tab);
        }
    }

    static class RectExtensions
    {
        /// <summary>Small helper: run an action on an object and return it (keeps layout code short).</summary>
        public static T Also<T>(this T obj, System.Action<T> action) { action(obj); return obj; }
    }
}
