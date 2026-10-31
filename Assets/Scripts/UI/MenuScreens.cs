using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace TiltRush
{
    /// <summary>Buttons pinned to the bottom of a screen.</summary>
    static class MenuUtil
    {
        public static void AddBack(Transform parent, UnityAction onClick) { AddButton(parent, "BACK", Neon.Gray, 0f, onClick); }

        public static void AddButton(Transform parent, string text, Color color, float x, UnityAction onClick, float width = 400f)
        {
            var b = UIKit.MakeButton(parent, text, color, new Vector2(width, 100), onClick, 46);
            UIKit.Anchor((RectTransform)b.transform, new Vector2(0.5f, 0f), new Vector2(x, 30), new Vector2(width, 100));
        }
    }

    public class MainMenuScreen : UIScreen
    {
        Text title;
        PlayerPreview preview;
        protected override bool HasVignette { get { return true; } }

        protected override void Build()
        {
            var st = new UIStack((RectTransform)transform, 60);
            title = st.Label(Config.GameTitle, 118, Color.white, 140);
            st.Label("SURVIVE THE DOTS", 30, Theme.TextDim, 40);

            preview = PlayerPreview.Create(transform);
            st.Place((RectTransform)preview.transform, 170, 170);
            st.Space(10);

            st.Button("PLAY", Neon.Lime, 660, 190, () => UI.Show<ModeSelectScreen>(), 92, true);
            st.Space(20);
            st.Row(110, 36,
                new UIStack.Item("OPTIONS", Neon.Blue, () => UI.Show<OptionsScreen>()),
                new UIStack.Item("HOW TO PLAY", Neon.Orange, () => UI.Show<HowToScreen>()),
                new UIStack.Item("WARDROBE", Neon.Purple, () => UI.Show<WardrobeScreen>()));
        }

        public override void OnShow() { preview.Refresh(); }

        // very soft colour drift instead of a rainbow
        void Update() { title.color = Color.HSVToRGB(Mathf.Repeat(Time.unscaledTime * 0.05f, 1f), 0.18f, 1f); }
    }

    public class ModeSelectScreen : UIScreen
    {
        Text onlineLabel, campaignLabel;
        protected override bool HasVignette { get { return true; } }

        protected override void Build()
        {
            new UIStack((RectTransform)transform, 40).Label("SELECT MODE", 80, Color.white, 100);

            const float w = 285f, gap = 16f;
            for (int i = 0; i < Modes.All.Length; i++)
            {
                var m = Modes.All[i];
                float x = (i - (Modes.All.Length - 1) / 2f) * (w + gap);

                var b = UIKit.MakeButton(transform, m.Name, m.Color, new Vector2(w, 300), () =>
                {
                    if (m.Id == GameModeId.Campaign) UI.Show<CampaignScreen>(); else App.I.StartMode(m);
                }, 38);
                UIKit.Anchor((RectTransform)b.transform, new Vector2(0.5f, 0.5f), new Vector2(x, 40), new Vector2(w, 300));
                if (m.Id == GameModeId.Online) onlineLabel = b.GetComponentInChildren<Text>();
                if (m.Id == GameModeId.Campaign) campaignLabel = b.GetComponentInChildren<Text>();

                var d = UIKit.Label(transform, m.Description, 26, Theme.TextDim);
                UIKit.Anchor(d.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(x, -190), new Vector2(w, 170));
            }
            MenuUtil.AddButton(transform, "BACK", Neon.Gray, -230f, () => UI.Show<MainMenuScreen>(), 420f);
            MenuUtil.AddButton(transform, "LEADERBOARD", Neon.Orange, 230f, () => UI.Show<LeaderboardScreen>(), 420f);
        }

        public override void OnShow()
        {
            onlineLabel.text = "ONLINE PVP\n<size=34>ELO " + SaveManager.Data.elo + "</size>";
            campaignLabel.text = "CAMPAIGN\n<size=34>" + SaveManager.Data.campaignDone + " / " + Campaign.Levels.Length + "</size>";
        }
    }

    public class OptionsScreen : UIScreen
    {
        Text trackText, shakeText;
        protected override bool HasVignette { get { return true; } }

        protected override void Build()
        {
            var d = SaveManager.Data;
            new UIStack((RectTransform)transform, 40).Label("OPTIONS", 80, Color.white, 100);

            UIKit.Panel(transform, -500, 150, 860, 520);
            UIKit.Panel(transform, 500, 150, 860, 580);

            var left = new UIStack((RectTransform)transform, 175, -500, 800);
            left.Slider("MUSIC VOLUME", 0f, 1f, d.musicVolume, v => { SaveManager.Data.musicVolume = v; App.I.Audio.ApplyVolumes(); });
            left.Slider("SFX VOLUME", 0f, 1f, d.sfxVolume, v => { SaveManager.Data.sfxVolume = v; App.I.Audio.ApplyVolumes(); });
            left.Space(10);
            left.Button("TILT SETUP", Neon.Blue, 800, 100, () => UI.Show<TiltSetupScreen>(), 42);

            var right = new UIStack((RectTransform)transform, 175, 500, 800);
            right.Spacing = 18;
            right.TextField("PLAYER NAME (LEADERBOARD)", d.playerName, 12, v =>
            {
                v = v.Trim();
                if (v.Length > 0) SaveManager.Data.playerName = v;
            });
            right.Space(6);
            shakeText = right.Button("", Neon.Blue, 800, 100, () => { SaveManager.Data.screenShake = !SaveManager.Data.screenShake; Refresh(); }, 40)
                             .GetComponentInChildren<Text>();
            trackText = right.Label("", 32, Color.white, 60);
            right.Button("NEXT TRACK", Neon.Indigo, 500, 100, () => { App.I.Audio.Next(); Refresh(); }, 42);

            MenuUtil.AddBack(transform, () => { SaveManager.Save(); UI.Show<MainMenuScreen>(); });
        }

        void Refresh()
        {
            shakeText.text = "SCREEN SHAKE: " + (SaveManager.Data.screenShake ? "ON" : "OFF");
            trackText.text = "Now playing: " + App.I.Audio.CurrentTrackName;
        }

        public override void OnShow() { Refresh(); }
    }

    public class HowToScreen : UIScreen
    {
        protected override bool HasVignette { get { return true; } }

        protected override void Build()
        {
            new UIStack((RectTransform)transform, 30).Label("HOW TO PLAY", 80, Color.white, 100);

            string col1 =
                "<color=#00FFFF>GOAL</color>\nSurvive! The red dots hunt you down.\n\n" +
                "<color=#00FFFF>CONTROLS</color>\nPhone: tilt your device. Choose your favourite pose in Options > Tilt Setup.\n" +
                "PC: WASD or arrow keys (click into the game window first). Co-op: P1 = WASD, P2 = arrows.\n\n" +
                "<color=#00FFFF>SCORE</color>\nPoints for every second you survive - more when it gets harder. " +
                "Bonus points for destroyed dots and pickups. Compare your Classic score on the leaderboard.";

            var sb = new System.Text.StringBuilder("<color=#00FFFF>PICKUPS</color>\n");
            foreach (var g in GadgetLibrary.All)
                sb.Append("<color=#").Append(ColorUtility.ToHtmlStringRGB(g.Color)).Append('>').Append(g.Name).Append("</color> - ").Append(g.Description).Append('\n');
            sb.Append("\n<color=#FFFF00>LAUNCH RINGS</color>\nYellow rings fling you backwards - escape danger or get thrown into it!");

            var sb3 = new System.Text.StringBuilder();
            sb3.Append("<color=#FF4040>RED WALLS</color>\nBars sweep across the arena. Slip around their ends!\n\n");
            sb3.Append("<color=#FF9090>SWORDS</color>\nA light red lane warns you - then a sword races along it.\n\n");
            sb3.Append("<color=#9FB4D2>OBSTACLES</color>\nGrey blocks (campaign) block you and the dots, but never hurt. Hide behind them!\n\n");
            sb3.Append("<color=#FF8FD0>DODGE MODE</color>\nThe dots do not chase you - they form shapes and waves. Watch out for the dotted lift, smileys and fireworks!\n\n");
            sb3.Append("<color=#00FFFF>MODES</color>\n");
            foreach (var m in Modes.All) sb3.Append(m.Name).Append(" - ").Append(m.Description).Append('\n');
            sb3.Append("\n<color=#00FFFF>UNLOCKS</color>\nComplete campaign levels to unlock new skins and colours.");

            Column(-640, col1);
            Column(0, sb.ToString());
            Column(640, sb3.ToString());
            MenuUtil.AddBack(transform, () => UI.Show<MainMenuScreen>());
        }

        void Column(float x, string text)
        {
            UIKit.Panel(transform, x, 140, 610, 790);
            var t = new UIStack((RectTransform)transform, 155, x, 560).Label(text, 30, Color.white, 750, TextAnchor.UpperLeft);
            t.resizeTextForBestFit = true; t.resizeTextMinSize = 16; t.resizeTextMaxSize = 30;
        }
    }

    public class WardrobeScreen : UIScreen
    {
        PlayerPreview preview;
        Button[] shapeBtns = new Button[8], bodyBtns, lineBtns;
        protected override bool HasVignette { get { return true; } }

        protected override void Build()
        {
            new UIStack((RectTransform)transform, 30).Label("WARDROBE", 80, Color.white, 100);

            // left: preview + hint
            UIKit.Panel(transform, -600, 150, 420, 420);
            var left = new UIStack((RectTransform)transform, 210, -600, 380);
            preview = PlayerPreview.Create(transform);
            left.Place((RectTransform)preview.transform, 300, 300);
            UIKit.Panel(transform, -600, 590, 420, 200);
            new UIStack((RectTransform)transform, 615, -600, 380)
                .Label("Complete campaign levels to unlock more skins and colours.", 28, Theme.TextDim, 160);

            // right: skins + colours
            UIKit.Panel(transform, 250, 150, 1120, 790);
            var st = new UIStack((RectTransform)transform, 170, 250, 1060);
            st.Spacing = 10;

            st.Label("SKIN", 40, Neon.Aqua, 50);
            for (int row = 0; row < 2; row++)
            {
                var items = new UIStack.Item[4];
                for (int c = 0; c < 4; c++)
                {
                    int idx = row * 4 + c;
                    items[c] = new UIStack.Item(Config.PlayerShapeNames[idx], Neon.Indigo, () =>
                    {
                        if (!Unlocks.ShapeUnlocked(idx)) return;
                        SaveManager.Data.playerShape = idx; Refresh();
                    });
                }
                var btns = st.Row(90, 32, items);
                for (int c = 0; c < 4; c++) shapeBtns[row * 4 + c] = btns[c];
            }
            st.Space(10);

            st.Label("BODY COLOR", 40, Neon.Aqua, 50);
            bodyBtns = Swatches(st, idx =>
            {
                if (!Unlocks.ColorUnlocked(idx)) return;
                SaveManager.Data.primary = idx; Refresh();
            });
            st.Space(10);

            st.Label("OUTLINE COLOR", 40, Neon.Aqua, 50);
            lineBtns = Swatches(st, idx =>
            {
                if (!Unlocks.ColorUnlocked(idx)) return;
                SaveManager.Data.outline = idx; Refresh();
            });

            MenuUtil.AddBack(transform, () => { SaveManager.Save(); UI.Show<MainMenuScreen>(); });
        }

        /// <summary>Two rows of colour swatches (10 each).</summary>
        static Button[] Swatches(UIStack st, System.Action<int> onPick)
        {
            int n = Config.Palette.Length, perRow = n / 2;
            var all = new Button[n];
            float oldSpacing = st.Spacing;
            st.Spacing = 8; st.RawColors = true;
            for (int row = 0; row < 2; row++)
            {
                var items = new UIStack.Item[perRow];
                for (int c = 0; c < perRow; c++)
                {
                    int idx = row * perRow + c;
                    items[c] = new UIStack.Item("", Config.Palette[idx], () => onPick(idx));
                }
                var btns = st.Row(62, 24, items);
                for (int c = 0; c < perRow; c++) all[row * perRow + c] = btns[c];
            }
            st.RawColors = false; st.Spacing = oldSpacing;
            return all;
        }

        static void SetText(Button b, string s) { b.GetComponentInChildren<Text>().text = s; }

        void Refresh()
        {
            var d = SaveManager.Data;
            for (int i = 0; i < shapeBtns.Length; i++)
            {
                bool open = Unlocks.ShapeUnlocked(i);
                SetText(shapeBtns[i], open ? Config.PlayerShapeNames[i] : "LV " + Unlocks.ShapeLevel[i]);
                if (open) UIKit.SetButtonColor(shapeBtns[i], i == d.playerShape ? Neon.Purple : Neon.Indigo);
                else UIKit.SetButtonColor(shapeBtns[i], Theme.Locked, true);
            }
            for (int i = 0; i < bodyBtns.Length; i++)
            {
                bool open = Unlocks.ColorUnlocked(i);
                string locked = "LV " + Unlocks.ColorLevel[i];
                SetText(bodyBtns[i], open ? (i == d.primary ? "OK" : "") : locked);
                SetText(lineBtns[i], open ? (i == d.outline ? "OK" : "") : locked);
                UIKit.SetButtonColor(bodyBtns[i], open ? Config.Palette[i] : Theme.Locked, true);
                UIKit.SetButtonColor(lineBtns[i], open ? Config.Palette[i] : Theme.Locked, true);
            }
            preview.Refresh();
        }

        public override void OnShow() { Refresh(); }
    }
}
