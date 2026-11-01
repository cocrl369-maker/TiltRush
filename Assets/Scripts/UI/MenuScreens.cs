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
            var play = UIKit.MakeButton(root, "", Neon.Lime, new Vector2(330, 330), () => UI.Show<ModeSelectScreen>().ResetView(), 40, true);
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

            var board = SideButton(root, 420, "LEADERBOARD", Neon.Orange, () => LeaderboardScreen.Open(null));
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

            var credits = UIKit.MakeButton(root, "CREDITS", Neon.Indigo, new Vector2(280, 100), () => UI.Show<CreditsScreen>(), 38);
            UIKit.Anchor((RectTransform)credits.transform, new Vector2(1f, 0f), new Vector2(-40, 40), new Vector2(280, 100));

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

    /// <summary>
    /// Mode selection in two steps: 4 big category cards (CAMPAIGN, CLASSIC, SPECIAL, MULTIPLAYER);
    /// CLASSIC / SPECIAL / MULTIPLAYER open two big cards for the modes inside.
    /// </summary>
    public class ModeSelectScreen : UIScreen
    {
        class Group { public string Title; public RectTransform Root; }

        RectTransform categories;
        Group[] groups;
        Text title, campaignInfo, classicInfo, specialInfo, multiInfo;
        Text normalBest, hardBest, dodgeBest, asteroidBest, onlineInfo;
        int open = -1;
        protected override bool HasVignette { get { return true; } }

        protected override void Build()
        {
            title = UIKit.Title(transform, "SELECT MODE", 96);
            UIKit.Anchor(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -30), new Vector2(1400, 120));

            // ---- step 1: categories
            categories = UIKit.Stretch(UIKit.Rect("Categories", transform));
            const float cw = 400f, ch = 580f;
            float[] xs = { -660f, -220f, 220f, 660f };
            Card(categories, xs[0], cw, ch, Neon.Blue, h => Icon(h, SpriteFactory.Fill(ShapeType.Star), 170, new Color(1f, 0.85f, 0.2f)),
                 "CAMPAIGN", "25 levels in 12 worlds", out campaignInfo, () => UI.Show<CampaignScreen>());
            Card(categories, xs[1], cw, ch, Neon.Lime, h => EnemyIcon(h, ShapeType.Saw, EnemyType.Normal, false, 170),
                 "CLASSIC", "Survive as long as you can", out classicInfo, () => Open(0));
            Card(categories, xs[2], cw, ch, Neon.Pink, h => RockIcon(h, 170),
                 "SPECIAL", "Dodge and Asteroid Field", out specialInfo, () => Open(1));
            Card(categories, xs[3], cw, ch, Neon.Aqua, h => TwoShips(h, 120),
                 "MULTIPLAYER", "Co-op on one phone or online", out multiInfo, () => Open(2));

            // ---- step 2: two big cards per group
            groups = new Group[3];
            groups[0] = NewGroup("CLASSIC", Modes.Classic, h => EnemyIcon(h, ShapeType.Saw, EnemyType.Normal, false, 190), out normalBest,
                                            Modes.Hard, h => EnemyIcon(h, ShapeType.Spike10, EnemyType.Big, true, 190), out hardBest);
            groups[1] = NewGroup("SPECIAL MODES", Modes.Dodge, h => Icon(h, SpriteFactory.Gadget(GadgetIcon.Orbit), 180, Color.white), out dodgeBest,
                                            Modes.Asteroids, h => RockIcon(h, 190), out asteroidBest);
            Text coopInfo;
            groups[2] = NewGroup("MULTIPLAYER", Modes.Coop, h => TwoShips(h, 130), out coopInfo,
                                            Modes.Online, h => Icon(h, SpriteFactory.Gadget(GadgetIcon.Crosshair), 180, Color.white), out onlineInfo);
            coopInfo.text = "1 phone, 2 players";

            MenuUtil.AddButton(transform, "BACK", Neon.Gray, -230f, Back, 420f);
            MenuUtil.AddButton(transform, "LEADERBOARD", Neon.Orange, 230f, () => LeaderboardScreen.Open(null), 420f);
        }

        Group NewGroup(string name, ModeDef a, System.Action<RectTransform> iconA, out Text infoA,
                                    ModeDef b, System.Action<RectTransform> iconB, out Text infoB)
        {
            var g = new Group { Title = name, Root = UIKit.Stretch(UIKit.Rect("Group" + name, transform)) };
            const float w = 660f, h = 580f;
            Card(g.Root, -360f, w, h, a.Color, iconA, a.Name, a.Description, out infoA, () => App.I.StartMode(a));
            Card(g.Root, 360f, w, h, b.Color, iconB, b.Name, b.Description, out infoB, () => App.I.StartMode(b));
            g.Root.gameObject.SetActive(false);
            return g;
        }

        /// <summary>Big card: picture on top, name, description, info line (best score, progress ...).</summary>
        static Button Card(RectTransform parent, float x, float w, float h, Color c, System.Action<RectTransform> picture,
                           string name, string desc, out Text info, UnityEngine.Events.UnityAction onClick)
        {
            var btn = UIKit.MakeButton(parent, "", c, new Vector2(w, h), onClick, 30);
            UIKit.Anchor((RectTransform)btn.transform, new Vector2(0.5f, 0.5f), new Vector2(x, -20), new Vector2(w, h));
            var holder = UIKit.Rect("Picture", btn.transform);
            holder.anchorMin = holder.anchorMax = holder.pivot = new Vector2(0.5f, 1f);
            holder.anchoredPosition = new Vector2(0, -50); holder.sizeDelta = new Vector2(220, 220);
            picture(holder);
            var t = UIKit.Label(btn.transform, name, 52, Color.white);
            UIKit.Anchor(t.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -285), new Vector2(w - 70, 84));
            UIKit.ShrinkToFit(t);
            var d = UIKit.Label(btn.transform, desc, 30, Theme.TextDim);
            UIKit.Anchor(d.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -375), new Vector2(w - 80, 120));
            UIKit.ShrinkToFit(d);
            info = UIKit.Label(btn.transform, "", 32, Neon.Yellow);
            UIKit.Anchor(info.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, 40), new Vector2(w - 70, 50));
            UIKit.ShrinkToFit(info);
            return btn;
        }

        // ---- pictures
        static void Icon(RectTransform h, Sprite s, float size, Color c)
        {
            UIKit.Icon(h, s, size, c).gameObject.AddComponent<Outline>().Also(o => { o.effectColor = Theme.Outline; o.effectDistance = new Vector2(4, -4); });
        }

        static void EnemyIcon(RectTransform h, ShapeType shape, EnemyType type, bool eyes, float size)
        {
            Color rim = GameTheme.EnemyRim(type);
            UIKit.Icon(h, SpriteFactory.Glow(), size * 1.4f, new Color(rim.r, rim.g, rim.b, 0.45f));
            UIKit.Icon(h, SpriteFactory.Fill(shape), size, GameTheme.EnemyBody);
            UIKit.Icon(h, SpriteFactory.Outline(shape), size, rim);
            if (eyes) UIKit.Icon(h, SpriteFactory.AngryEyes(), size * 0.58f, GameTheme.EnemyEye);
            else UIKit.Icon(h, SpriteFactory.Fill(ShapeType.Circle), size * 0.24f, GameTheme.EnemyEye);
        }

        static void RockIcon(RectTransform h, float size)
        {
            Color rim = new Color32(235, 200, 160, 255);
            UIKit.Icon(h, SpriteFactory.Glow(), size * 1.4f, new Color(1f, 0.6f, 0.3f, 0.4f));
            UIKit.Icon(h, SpriteFactory.Fill(ShapeType.Rock), size, new Color32(58, 44, 40, 255));
            UIKit.Icon(h, SpriteFactory.Craters(), size * 0.9f, new Color(rim.r, rim.g, rim.b, 0.5f));
            UIKit.Icon(h, SpriteFactory.Outline(ShapeType.Rock), size, rim);
        }

        static void TwoShips(RectTransform h, float size)
        {
            for (int i = 0; i < 2; i++)
            {
                var st = PlayerStyle.FromSave(i);
                var holder = UIKit.Rect("Ship", h);
                holder.anchorMin = holder.anchorMax = holder.pivot = new Vector2(0.5f, 0.5f);
                holder.sizeDelta = Vector2.one * size;
                holder.anchoredPosition = new Vector2(i == 0 ? -size * 0.42f : size * 0.42f, 0);
                holder.localRotation = Quaternion.Euler(0, 0, i == 0 ? 20f : -20f);
                UIKit.Icon(holder, SpriteFactory.Fill(st.Shape), size * 1.25f, new Color(0, 0, 0, 0.7f));
                UIKit.Icon(holder, SpriteFactory.Fill(st.Shape), size, st.Primary);
                if (st.Detail != null) UIKit.Icon(holder, st.Detail, size, st.Outline);
                UIKit.Icon(holder, SpriteFactory.Outline(st.Shape), size, st.Outline);
            }
        }

        // ---- navigation
        void Open(int g)
        {
            open = g;
            categories.gameObject.SetActive(g < 0);
            for (int i = 0; i < groups.Length; i++) groups[i].Root.gameObject.SetActive(i == g);
            title.text = g < 0 ? "SELECT MODE" : groups[g].Title;
        }

        void Back()
        {
            if (open >= 0) Open(-1);
            else UI.Show<MainMenuScreen>();
        }

        public override void OnShow()
        {
            var d = SaveManager.Data;
            campaignInfo.text = d.campaignDone + " / " + Campaign.Levels.Length + " DONE";
            classicInfo.text = "BEST " + d.bestClassicScore.ToString("N0");
            specialInfo.text = "BEST " + Mathf.Max(d.bestDodgeScore, d.bestAsteroidsScore).ToString("N0");
            multiInfo.text = "ELO " + d.elo;
            normalBest.text = "BEST " + d.bestClassicScore.ToString("N0");
            hardBest.text = "BEST " + d.bestHardScore.ToString("N0");
            dodgeBest.text = "BEST " + d.bestDodgeScore.ToString("N0");
            asteroidBest.text = "BEST " + d.bestAsteroidsScore.ToString("N0");
            onlineInfo.text = "ELO " + d.elo;
        }

        /// <summary>Shows the categories again unless we come back from a game of the open group (then stay there).</summary>
        public void ResetView() { Open(-1); }
    }
}
