using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace TiltRush
{
    public class HudScreen : UIScreen
    {
        RectTransform safe;
        Text timeLabel, pointsLabel, scoreLabel, centerLabel, toastLabel, effectLabel;
        float toastTimer, bonusTimer;
        int bonusSum;
        Text bonusLabel;
        RectTransform pauseRt;

        // tutorial: BLIP's speech bubble
        RectTransform speechRoot, blip;
        Text speechText, tutTimer;
        GameObject skipBtn;
        string speechFull = "";
        float speechShown;

        protected override void Build()
        {
            safe = UIKit.Stretch(UIKit.Rect("Safe", transform));

            timeLabel = UIKit.Label(safe, "0.0s", 64, Color.white, TextAnchor.UpperLeft);
            UIKit.Anchor(timeLabel.rectTransform, new Vector2(0, 1), new Vector2(40, -30), new Vector2(360, 90));

            pointsLabel = UIKit.Label(safe, "", 44, Neon.Yellow, TextAnchor.UpperLeft);
            UIKit.Anchor(pointsLabel.rectTransform, new Vector2(0, 1), new Vector2(40, -95), new Vector2(500, 60));

            bonusLabel = UIKit.Label(safe, "", 38, Neon.Green, TextAnchor.UpperLeft);
            UIKit.Anchor(bonusLabel.rectTransform, new Vector2(0, 1), new Vector2(40, -150), new Vector2(400, 50));

            scoreLabel = UIKit.Label(safe, "", 40, Color.white);
            UIKit.Anchor(scoreLabel.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -130), new Vector2(900, 60));

            effectLabel = UIKit.Label(safe, "", 34, Neon.Aqua, TextAnchor.UpperLeft);
            UIKit.Anchor(effectLabel.rectTransform, new Vector2(0, 1), new Vector2(40, -205), new Vector2(500, 160));

            centerLabel = UIKit.Label(safe, "", 260, Color.white);
            UIKit.Anchor(centerLabel.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(600, 400));

            toastLabel = UIKit.Label(safe, "", 90, Color.white);
            UIKit.Anchor(toastLabel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 330), new Vector2(1000, 140));

            var pause = UIKit.MakeButton(safe, "II", Neon.Blue, new Vector2(120, 120), () => App.I.Pause(), 56);
            pauseRt = (RectTransform)pause.transform;

            BuildSpeech();
        }

        void BuildSpeech()
        {
            speechRoot = UIKit.Panel(safe, 0, 0, 1380, 200);
            UIKit.Anchor(speechRoot, new Vector2(0.5f, 0f), new Vector2(-110, 24), new Vector2(1380, 200));

            // BLIP: friendly aqua dot with big eyes
            blip = UIKit.Rect("Blip", speechRoot);
            UIKit.Anchor(blip, new Vector2(0f, 0.5f), new Vector2(40, 0), new Vector2(140, 140));
            blip.pivot = new Vector2(0f, 0.5f);
            UIKit.Icon(blip, SpriteFactory.Glow(), 220, new Color(0f, 1f, 1f, 0.35f));
            UIKit.Icon(blip, SpriteFactory.Fill(ShapeType.Circle), 130, new Color(0, 0, 0, 0.8f));
            UIKit.Icon(blip, SpriteFactory.Fill(ShapeType.Circle), 116, new Color32(40, 220, 230, 255));
            UIKit.Icon(blip, SpriteFactory.Outline(ShapeType.Circle), 116, Color.white);
            for (int s = -1; s <= 1; s += 2)
            {
                var eye = UIKit.Icon(blip, SpriteFactory.Fill(ShapeType.Circle), 34, Color.white);
                eye.rectTransform.anchoredPosition = new Vector2(s * 22, 12);
                var pupil = UIKit.Icon(eye.transform, SpriteFactory.Fill(ShapeType.Circle), 17, new Color(0.05f, 0.05f, 0.1f));
                pupil.rectTransform.anchoredPosition = new Vector2(3, -2);
            }
            var smile = UIKit.Icon(blip, SpriteFactory.Fill(ShapeType.Circle), 30, new Color(0.05f, 0.05f, 0.1f));
            smile.rectTransform.sizeDelta = new Vector2(34, 16); smile.rectTransform.anchoredPosition = new Vector2(0, -22);

            speechText = UIKit.Label(speechRoot, "", 40, Color.white, TextAnchor.MiddleLeft);
            var rt = speechText.rectTransform;
            rt.anchorMin = new Vector2(0f, 0f); rt.anchorMax = new Vector2(1f, 1f);
            rt.offsetMin = new Vector2(210, 20); rt.offsetMax = new Vector2(-40, -20);

            var skip = UIKit.MakeButton(safe, "SKIP", Neon.Gray, new Vector2(200, 100), () => App.I.Session.SkipTutorial(), 40);
            UIKit.Anchor((RectTransform)skip.transform, new Vector2(1f, 0f), new Vector2(-30, 74), new Vector2(200, 100));
            skipBtn = skip.gameObject;

            tutTimer = UIKit.Label(safe, "", 80, Neon.Yellow);
            UIKit.Anchor(tutTimer.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -190), new Vector2(900, 110));
        }

        void OnSpeech(string text) { speechFull = text; speechShown = 0f; }
        void OnTutorialTimer(float s) { tutTimer.text = s < 0f ? "" : "SURVIVE: " + Mathf.CeilToInt(s); }

        /// <summary>Options > Interface: HUD size and pause button side.</summary>
        void ApplyLayout()
        {
            var d = SaveManager.Data;
            float k = d.hudSize == 0 ? 0.8f : (d.hudSize == 2 ? 1.25f : 1f);
            bool left = d.pauseLeft;

            // the info column moves to the other side when the pause button is on the left
            Vector2 corner = left ? new Vector2(1, 1) : new Vector2(0, 1);
            float sx = left ? -40f : 40f;
            TextAnchor align = left ? TextAnchor.UpperRight : TextAnchor.UpperLeft;
            Text[] col = { timeLabel, pointsLabel, bonusLabel, effectLabel };
            float[] ys = { -30f, -95f, -150f, -205f };
            for (int i = 0; i < col.Length; i++)
            {
                var rt = col[i].rectTransform;
                rt.anchorMin = rt.anchorMax = rt.pivot = corner;
                rt.anchoredPosition = new Vector2(sx, ys[i] * k);
                rt.localScale = Vector3.one * k;
                col[i].alignment = align;
            }
            scoreLabel.rectTransform.localScale = Vector3.one * k;
            toastLabel.rectTransform.localScale = Vector3.one * k;

            Vector2 pc = left ? new Vector2(0, 1) : new Vector2(1, 1);
            UIKit.Anchor(pauseRt, pc, new Vector2(left ? 40 : -40, -30), new Vector2(120, 120));
            pauseRt.localScale = Vector3.one * k;
        }

        void OnEnable() { GameWorld.Toast += OnToast; GameWorld.Bonus += OnBonus; GameWorld.Speech += OnSpeech; GameWorld.TutorialTimer += OnTutorialTimer; }
        void OnDisable() { GameWorld.Toast -= OnToast; GameWorld.Bonus -= OnBonus; GameWorld.Speech -= OnSpeech; GameWorld.TutorialTimer -= OnTutorialTimer; }

        // bonus points that arrive within ~1.4 s of each other are shown as one growing "+N"
        void OnBonus(int pts)
        {
            if (bonusTimer <= 0f) bonusSum = 0;
            bonusSum += pts;
            bonusTimer = 1.4f;
            bonusLabel.text = "+" + bonusSum;
        }

        void OnToast(string text, Color c) { toastLabel.text = text; toastLabel.color = c; toastTimer = 1.5f; }

        public override void OnShow()
        {
            Rect r = Screen.safeArea;
            safe.anchorMin = new Vector2(r.xMin / Screen.width, r.yMin / Screen.height);
            safe.anchorMax = new Vector2(r.xMax / Screen.width, r.yMax / Screen.height);
            safe.offsetMin = safe.offsetMax = Vector2.zero;
            toastLabel.text = ""; toastTimer = 0;
            bonusLabel.text = ""; bonusTimer = 0;
            speechFull = ""; speechText.text = ""; tutTimer.text = "";
            ApplyLayout();
        }

        void Update()
        {
            var w = GameWorld.Current;
            if (w == null) return;

            bool tut = w.Mode.Id == GameModeId.Tutorial;
            speechRoot.gameObject.SetActive(tut);
            skipBtn.SetActive(tut);
            if (tut)
            {
                // typewriter effect + BLIP bobs while talking
                speechShown += Time.unscaledDeltaTime * 55f;
                int n = Mathf.Min(speechFull.Length, Mathf.FloorToInt(speechShown));
                speechText.text = speechFull.Substring(0, n);
                bool talking = n < speechFull.Length;
                blip.anchoredPosition = new Vector2(40, talking ? 6f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 14f)) : 3f * Mathf.Sin(Time.unscaledTime * 2f));
            }

            timeLabel.text = tut ? "" : (w.Level != null ? w.TimeLeft : w.Elapsed).ToString("0.0") + "s";    // campaign: time left
            pointsLabel.text = (w.Mode.Id == GameModeId.Classic || w.Mode.Id == GameModeId.Hard || w.Mode.Id == GameModeId.Dodge) ? w.ScoreInt.ToString("N0") + " PTS" : "";
            scoreLabel.text = App.I.Session.ScoreLine;
            centerLabel.text = w.State == GameWorld.Phase.Countdown ? Mathf.CeilToInt(w.CountdownLeft).ToString() : "";

            var sb = new StringBuilder();
            if (w.SlowTimer > 0) sb.AppendLine("SLOW-MO " + w.SlowTimer.ToString("0.0"));
            if (w.FreezeTimer > 0) sb.AppendLine("FREEZE " + w.FreezeTimer.ToString("0.0"));
            foreach (var p in w.Players)
            {
                string who = w.Players.Count > 1 ? "P" + (p.Index + 1) + " " : "";
                if (p.ShieldTime > 0) sb.AppendLine(who + "SHIELD " + p.ShieldTime.ToString("0.0"));
                if (p.StormTime > 0) sb.AppendLine(who + "STORM " + p.StormTime.ToString("0.0"));
                if (p.ShrinkTime > 0) sb.AppendLine(who + "SHRINK " + p.ShrinkTime.ToString("0.0"));
                if (p.BoostTime > 0) sb.AppendLine(who + "BOOST " + p.BoostTime.ToString("0.0"));
                if (p.OrbitTime > 0) sb.AppendLine(who + "ORBIT " + p.OrbitTime.ToString("0.0"));
            }
            effectLabel.text = sb.ToString();

            bonusTimer -= Time.unscaledDeltaTime;
            var bc = bonusLabel.color; bc.a = Mathf.Clamp01(bonusTimer / 0.4f); bonusLabel.color = bc;

            toastTimer -= Time.unscaledDeltaTime;
            var c = toastLabel.color; c.a = Mathf.Clamp01(toastTimer / 0.5f); toastLabel.color = c;
        }
    }

    public class PauseScreen : UIScreen
    {
        protected override void Build()
        {
            var bg = ((RectTransform)transform).gameObject.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.8f);

            var st = new UIStack((RectTransform)transform, 250);
            st.Label("PAUSED", 120, Color.white, 160);
            st.Space(10);
            st.Button("RESUME", Neon.Lime, 700, 160, () => App.I.Resume(), 72);
            st.Button("QUIT", Neon.Red, 700, 130, () => App.I.QuitToMenu(), 60);
        }
    }

    /// <summary>Generic message screen: title, text, 0-3 buttons. Used for results, matchmaking, errors.</summary>
    public class DialogScreen : UIScreen
    {
        Text titleLabel, bodyLabel;
        RectTransform buttonRoot;

        protected override void Build()
        {
            var bg = ((RectTransform)transform).gameObject.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.75f);

            var st = new UIStack((RectTransform)transform, 70);
            titleLabel = st.Label("", 90, Color.white, 150);
            bodyLabel = st.Label("", 46, Color.white, 400);

            buttonRoot = UIKit.Stretch(UIKit.Rect("Buttons", transform));
            buttonRoot.offsetMax = new Vector2(0, -690);
        }

        public void Set(string title, string body, params UIStack.Item[] items)
        {
            titleLabel.text = title;
            bodyLabel.text = body;
            foreach (Transform c in buttonRoot) Destroy(c.gameObject);
            if (items.Length == 0) return;
            var st = new UIStack(buttonRoot, 0, 0, items.Length * 500f + (items.Length - 1) * 24f);
            st.Row(120, 46, items);                // buttons side by side (up to 3 fit on screen)
        }
    }
}
