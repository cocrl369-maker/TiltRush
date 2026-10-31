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

    /// <summary>
    /// Main menu in Geometry-Dash style: big logo, a large PLAY button with a play symbol in the middle,
    /// WARDROBE (shows your ship) and LEADERBOARD left and right, small round buttons for options and help,
    /// your stats at the bottom. The colourful background slowly travels through all theme colours.
    /// </summary>
    public class MainMenuScreen : UIScreen
    {
        Text title, stats, campaignInfo;
        PlayerPreview preview;
        RectTransform logo;
        protected override bool HasVignette { get { return true; } }

        protected override void Build()
        {
            var root = (RectTransform)transform;

            // logo
            logo = UIKit.Rect("Logo", root);
            UIKit.Anchor(logo, new Vector2(0.5f, 1f), new Vector2(0, -150), new Vector2(1400, 200));
            logo.pivot = new Vector2(0.5f, 0.5f);
            title = UIKit.Title(logo, Config.GameTitle, 170);
            UIKit.Stretch(title.rectTransform);
            var sub = UIKit.Label(root, "SURVIVE THE SPIKES", 36, Theme.TextDim);
            UIKit.Anchor(sub.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -265), new Vector2(900, 50));

            // big PLAY button with a play symbol
            var play = UIKit.MakeButton(root, "", Neon.Lime, new Vector2(330, 330), () => UI.Show<ModeSelectScreen>(), 40, true);
            UIKit.Anchor((RectTransform)play.transform, new Vector2(0.5f, 0.5f), new Vector2(0, -20), new Vector2(330, 330));
            var tri = UIKit.Icon(play.transform, SpriteFactory.Fill(ShapeType.Triangle), 190, Color.white);
            tri.rectTransform.localRotation = Quaternion.Euler(0, 0, -90);
            tri.rectTransform.anchoredPosition = new Vector2(14, 10);
            tri.gameObject.AddComponent<Outline>().Also(o => { o.effectColor = Theme.Outline; o.effectDistance = new Vector2(5, -5); });
            var playText = play.GetComponentInChildren<Text>();
            playText.text = "PLAY"; playText.alignment = TextAnchor.LowerCenter; playText.rectTransform.offsetMin = new Vector2(0, 18);

            // wardrobe (with your ship) and leaderboard
            var wardrobe = SideButton(root, -420, "WARDROBE", Neon.Purple, () => UI.Show<WardrobeScreen>());
            preview = PlayerPreview.Create(wardrobe.transform);
            var prt = (RectTransform)preview.transform;
            prt.anchorMin = prt.anchorMax = prt.pivot = new Vector2(0.5f, 0.5f);
            prt.sizeDelta = new Vector2(120, 120); prt.anchoredPosition = new Vector2(0, 22);

            var board = SideButton(root, 420, "LEADERBOARD", Neon.Orange, () => UI.Show<LeaderboardScreen>());
            var star = UIKit.Icon(board.transform, SpriteFactory.Fill(ShapeType.Star), 120, new Color(1f, 0.9f, 0.25f, 1f));
            star.rectTransform.anchoredPosition = new Vector2(0, 22);
            star.gameObject.AddComponent<Outline>().Also(o => { o.effectColor = Theme.Outline; o.effectDistance = new Vector2(4, -4); });

            // small round buttons at the bottom
            var opt = RoundButton(root, -110, Neon.Blue, () => UI.Show<OptionsScreen>());
            var gear = UIKit.Icon(opt.transform, SpriteFactory.Gear(), 84, Color.white);
            gear.gameObject.AddComponent<Spin>().Speed = -25f;
            var help = RoundButton(root, 110, Neon.Aqua, () => UI.Show<HowToScreen>());
            help.GetComponentInChildren<Text>().text = "?";
            help.GetComponentInChildren<Text>().fontSize = 76;

            campaignInfo = UIKit.Label(root, "", 30, Theme.TextDim);
            UIKit.Anchor(campaignInfo.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, -235), new Vector2(700, 44));

            stats = UIKit.Label(root, "", 30, Theme.TextDim, TextAnchor.LowerLeft);
            UIKit.Anchor(stats.rectTransform, new Vector2(0f, 0f), new Vector2(40, 30), new Vector2(900, 50));
        }

        static Button SideButton(RectTransform root, float x, string text, Color c, UnityEngine.Events.UnityAction a)
        {
            var b = UIKit.MakeButton(root, text, c, new Vector2(260, 260), a, 30);
            UIKit.Anchor((RectTransform)b.transform, new Vector2(0.5f, 0.5f), new Vector2(x, -20), new Vector2(260, 260));
            var t = b.GetComponentInChildren<Text>();
            t.alignment = TextAnchor.LowerCenter; t.rectTransform.offsetMin = new Vector2(0, 20);
            return b;
        }

        static Button RoundButton(RectTransform root, float x, Color c, UnityEngine.Events.UnityAction a)
        {
            var b = UIKit.MakeButton(root, "", c, new Vector2(150, 150), a, 40);
            UIKit.Anchor((RectTransform)b.transform, new Vector2(0.5f, 0f), new Vector2(x, 40), new Vector2(150, 150));
            return b;
        }

        public override void OnShow()
        {
            preview.Refresh();
            var d = SaveManager.Data;
            stats.text = "BEST " + d.bestClassicScore.ToString("N0") + "   •   ELO " + d.elo + "   •   SKINS " + Skins.UnlockedCount + "/" + Skins.All.Length;
            campaignInfo.text = "CAMPAIGN " + d.campaignDone + " / " + Campaign.Levels.Length;
        }

        void Update()
        {
            // logo: colour follows the background theme, gentle bob and tilt
            float t = Time.unscaledTime;
            title.color = Color.Lerp(Color.white, GameTheme.Now.Accent, 0.45f);
            logo.anchoredPosition = new Vector2(0, -150 + 8f * Mathf.Sin(t * 1.6f));
            logo.localRotation = Quaternion.Euler(0, 0, 1.5f * Mathf.Sin(t * 0.9f));
        }
    }

    /// <summary>Rotates a UI element (gear icon).</summary>
    public class Spin : MonoBehaviour
    {
        public float Speed = 30f;
        void Update() { transform.Rotate(0, 0, Speed * Time.unscaledDeltaTime); }
    }

    public class ModeSelectScreen : UIScreen
    {
        Text onlineLabel, campaignLabel;
        protected override bool HasVignette { get { return true; } }

        protected override void Build()
        {
            UIKit.Anchor(UIKit.Title(transform, "SELECT MODE", 96).rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -30), new Vector2(1200, 120));

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

    public class HowToScreen : UIScreen
    {
        protected override bool HasVignette { get { return true; } }

        protected override void Build()
        {
            new UIStack((RectTransform)transform, 30).Label("HOW TO PLAY", 80, Color.white, 100);

            string col1 =
                "<color=#00FFFF>GOAL</color>\nSurvive! The spiky dots hunt you down - saws, fast stars and big angry spike balls. The harder it gets, the more colourful the world.\n\n" +
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
            sb3.Append("<color=#9FB4D2>OBSTACLES</color>\nDark blocks (campaign) block you and the dots, but never hurt. Hide behind them!\n\n");
            sb3.Append("<color=#FF8FD0>DODGE MODE</color>\nThe dots do not chase you - they form shapes and waves. Watch out for the dotted lift, smileys and fireworks!\n\n");
            sb3.Append("<color=#00FFFF>MODES</color>\n");
            foreach (var m in Modes.All) sb3.Append(m.Name).Append(" - ").Append(m.Description).Append('\n');
            sb3.Append("\n<color=#00FFFF>UNLOCKS</color>\nColours: campaign levels. Skins: campaign, high scores, Dodge times, destroyed dots, pickups and online wins - see the Wardrobe.");

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
}
