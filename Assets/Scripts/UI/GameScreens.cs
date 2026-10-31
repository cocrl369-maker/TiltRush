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
        }

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

        void OnEnable() { GameWorld.Toast += OnToast; GameWorld.Bonus += OnBonus; }
        void OnDisable() { GameWorld.Toast -= OnToast; GameWorld.Bonus -= OnBonus; }

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
            ApplyLayout();
        }

        void Update()
        {
            var w = GameWorld.Current;
            if (w == null) return;

            timeLabel.text = (w.Level != null ? w.TimeLeft : w.Elapsed).ToString("0.0") + "s";    // campaign: time left
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
