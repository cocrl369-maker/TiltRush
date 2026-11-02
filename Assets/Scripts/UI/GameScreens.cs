using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace TiltRush
{
    public class HudScreen : UIScreen
    {
        public override string MusicContext { get { return null; } }
        RectTransform safe;
        Text timeLabel, pointsLabel, scoreLabel, centerLabel, toastLabel, effectLabel;
        float toastTimer, bonusTimer;
        int bonusSum;
        Text bonusLabel;
        RectTransform pauseRt;
        RectTransform heartRow;
        Image[] hearts = new Image[5];

        // tutorial: BLIP's speech bubble
        RectTransform speechRoot, blip;
        Text speechText, tutTimer;
        GameObject skipBtn;
        string speechFull = "";

        // co-op on one phone: thumb sticks
        RectTransform[] stickBase = new RectTransform[2], stickKnob = new RectTransform[2];
        Text[] stickLabel = new Text[2];
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
            BuildSticks();

            // lives (Dodge / Asteroid field): hearts at the top centre
            heartRow = UIKit.Rect("Hearts", safe);
            UIKit.Anchor(heartRow, new Vector2(0.5f, 1f), new Vector2(0, -28), new Vector2(400, 70));
            for (int i = 0; i < hearts.Length; i++)
            {
                var back = UIKit.Icon(heartRow, SpriteFactory.Heart(), 66, new Color(0, 0, 0, 0.75f));
                back.rectTransform.anchoredPosition = new Vector2((i - 1) * 76f, 0);
                hearts[i] = UIKit.Icon(back.transform, SpriteFactory.Heart(), 54, Neon.Red);
            }
        }

        void BuildSticks()
        {
            for (int i = 0; i < 2; i++)
            {
                stickBase[i] = UIKit.Rect("StickBase" + i, transform);
                stickBase[i].anchorMin = stickBase[i].anchorMax = stickBase[i].pivot = new Vector2(0.5f, 0.5f);
                stickBase[i].sizeDelta = new Vector2(10, 10);
                UIKit.Icon(stickBase[i], SpriteFactory.Fill(ShapeType.Circle), 100, new Color(0, 0, 0, 0.35f)).rectTransform.Also(r => UIKit.Stretch(r));
                UIKit.Icon(stickBase[i], SpriteFactory.Outline(ShapeType.Circle), 100, new Color(1, 1, 1, 0.6f)).rectTransform.Also(r => UIKit.Stretch(r));
                stickLabel[i] = UIKit.Label(stickBase[i], "P" + (i + 1), 34, Color.white);
                UIKit.Stretch(stickLabel[i].rectTransform);
                stickKnob[i] = UIKit.Rect("Knob" + i, transform);
                stickKnob[i].anchorMin = stickKnob[i].anchorMax = stickKnob[i].pivot = new Vector2(0.5f, 0.5f);
                stickKnob[i].sizeDelta = new Vector2(10, 10);
                UIKit.Icon(stickKnob[i], SpriteFactory.Fill(ShapeType.Circle), 100, Color.white).rectTransform.Also(r => UIKit.Stretch(r));
            }
        }

        /// <summary>Draws the thumb sticks; when nobody touches, a faint stick shows where to put the thumb.</summary>
        void UpdateSticks()
        {
            var root = (RectTransform)transform;
            var sticks = TouchStickInput.All;
            for (int i = 0; i < 2; i++)
            {
                bool on = i < sticks.Count;
                stickBase[i].gameObject.SetActive(on);
                stickKnob[i].gameObject.SetActive(on);
                if (!on) continue;
                var s = sticks[i];
                Color c = PlayerStyle.FromSave(i).Primary;
                float rPx = TouchStickInput.RadiusPx;
                Vector2 origin = s.Held ? s.Origin : new Vector2(s.Zone == 1 ? Screen.width * 0.82f : Screen.width * 0.18f, Screen.height * 0.25f);
                Vector2 knob = s.Held ? s.Knob : origin;
                Vector2 o, k, edge;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(root, origin, null, out o);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(root, knob, null, out k);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(root, origin + Vector2.right * rPx, null, out edge);
                float r = (edge - o).magnitude;
                stickBase[i].anchoredPosition = o; stickBase[i].sizeDelta = Vector2.one * r * 2f;
                stickKnob[i].anchoredPosition = k; stickKnob[i].sizeDelta = Vector2.one * r * 0.8f;
                float a = s.Held ? 1f : 0.45f;
                foreach (var img in stickKnob[i].GetComponentsInChildren<Image>()) img.color = new Color(c.r, c.g, c.b, 0.85f * a);
                SetAlpha(stickBase[i], a);
                stickLabel[i].enabled = !s.Held;
            }
        }

        void UpdateHearts(GameWorld w)
        {
            int max = w.Mode.Lives;
            heartRow.gameObject.SetActive(max > 1 && w.Players.Count > 0);
            if (max <= 1 || w.Players.Count == 0) return;
            int left = w.Players[0].Alive ? w.Players[0].Lives : 0;
            for (int i = 0; i < hearts.Length; i++)
            {
                var back = hearts[i].transform.parent.gameObject;
                back.SetActive(i < max);
                ((RectTransform)back.transform).anchoredPosition = new Vector2((i - (max - 1) / 2f) * 76f, 0);
                bool full = i < left;
                hearts[i].color = full ? (Color)new Color32(255, 60, 90, 255) : new Color(1f, 1f, 1f, 0.12f);
                // the last heart beats
                hearts[i].rectTransform.localScale = Vector3.one * (full && left == 1 ? 1f + 0.15f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 6f)) : 1f);
            }
        }

        static void SetAlpha(RectTransform rt, float a)
        {
            var g = rt.GetComponent<CanvasGroup>();
            if (g == null) g = rt.gameObject.AddComponent<CanvasGroup>();
            g.alpha = a; g.blocksRaycasts = false;
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

            UpdateSticks();
            UpdateHearts(w);
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
            pointsLabel.text = (w.Mode.Id == GameModeId.Classic || w.Mode.Id == GameModeId.Hard || w.Mode.Id == GameModeId.Dodge || w.Mode.Id == GameModeId.Asteroids) ? w.ScoreInt.ToString("N0") + " PTS" : "";
            scoreLabel.text = App.I.Session.ScoreLine;
            centerLabel.text = w.State == GameWorld.Phase.Countdown ? Mathf.CeilToInt(w.CountdownLeft).ToString() : "";

            var sb = new StringBuilder();
            if (w.SlowTimer > 0) sb.AppendLine("SLOW-MO " + w.SlowTimer.ToString("0.0"));
            foreach (var p in w.Players)
            {
                string who = w.Players.Count > 1 ? "P" + (p.Index + 1) + " " : "";
                if (p.ShieldTime > 0) sb.AppendLine(who + "SHIELD " + p.ShieldTime.ToString("0.0"));
                if (p.StormTime > 0) sb.AppendLine(who + "STORM " + p.StormTime.ToString("0.0"));
                if (p.ShrinkTime > 0) sb.AppendLine(who + "SHRINK " + p.ShrinkTime.ToString("0.0"));
                if (p.BoostTime > 0) sb.AppendLine(who + "BOOST " + p.BoostTime.ToString("0.0"));
                if (p.OrbitTime > 0) sb.AppendLine(who + "ORBIT " + p.OrbitTime.ToString("0.0"));
                if (p.SpreadTime > 0) sb.AppendLine(who + "SPREAD SHOT " + p.SpreadTime.ToString("0.0"));
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
        public override string MusicContext { get { return null; } }
        protected override void Build()
        {
            var bg = ((RectTransform)transform).gameObject.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.8f);

            var st = new UIStack((RectTransform)transform, 140);
            st.Label("PAUSED", 120, Color.white, 160);
            st.Space(6);
            st.Button("RESUME", Neon.Lime, 700, 150, () => App.I.Resume(), 72);
            st.Button("CONTROLS", Neon.Green, 700, 118, () => ControlsScreen.Open(() => App.I.BackToPause()), 56);   // game stays paused
            st.Button("OPTIONS", Neon.Blue, 700, 118, () => UI.Show<OptionsScreen>(), 56);
            st.Button("QUIT", Neon.Red, 700, 118, () => App.I.QuitToMenu(), 56);
        }
    }

    /// <summary>Generic message screen: title, text, 0-3 buttons. Used for results, matchmaking, errors.</summary>
    public class DialogScreen : UIScreen
    {
        public override string MusicContext { get { return null; } }
        Text titleLabel, bodyLabel;
        RectTransform buttonRoot;

        protected override void Build()
        {
            var bg = ((RectTransform)transform).gameObject.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.75f);

            var st = new UIStack((RectTransform)transform, 70);
            titleLabel = st.Label("", 90, Color.white, 150);
            bodyLabel = st.Label("", 46, Color.white, 400);
            UIKit.ShrinkToFit(bodyLabel);                 // long texts get smaller instead of running into the buttons
            UIKit.ShrinkToFit(titleLabel);

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
