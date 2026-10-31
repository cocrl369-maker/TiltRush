using UnityEngine;
using UnityEngine.UI;

namespace TiltRush
{
    /// <summary>Classic-mode leaderboard: top 10 plus your own rank if you are not in the top 10.</summary>
    public class LeaderboardScreen : UIScreen
    {
        const int Rows = 11;   // 10 + one extra row for "you"
        Text[] rankT = new Text[Rows], nameT = new Text[Rows], scoreT = new Text[Rows];
        Text status;
        protected override bool HasVignette { get { return true; } }

        protected override void Build()
        {
            new UIStack((RectTransform)transform, 25).Label("CLASSIC LEADERBOARD", 70, Color.white, 90);

            status = UIKit.Label(transform, "", 36, new Color(1, 1, 1, 0.8f));
            UIKit.Anchor(status.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -125), new Vector2(1000, 50));

            for (int i = 0; i < Rows; i++)
            {
                float y = -175f - i * 62f - (i == Rows - 1 ? 20f : 0f);
                rankT[i] = Cell(-420f, y, 140f, TextAnchor.MiddleRight);
                nameT[i] = Cell(-60f, y, 520f, TextAnchor.MiddleLeft);
                scoreT[i] = Cell(380f, y, 240f, TextAnchor.MiddleRight);
            }
            MenuUtil.AddBack(transform, () => UI.Show<ModeSelectScreen>());
        }

        Text Cell(float x, float y, float w, TextAnchor align)
        {
            var t = UIKit.Label(transform, "", 38, Color.white, align);
            UIKit.Anchor(t.rectTransform, new Vector2(0.5f, 1f), new Vector2(x, y), new Vector2(w, 58));
            return t;
        }

        public override void OnShow()
        {
            Clear();
            status.text = "Loading...";
            App.I.Online.GetLeaderboard("classic", 10,
                entries =>
                {
                    if (!gameObject.activeInHierarchy) return;
                    status.text = SaveManager.Data.bestClassicScore > 0 ? "" : "Play a Classic run to get on the board!";
                    for (int i = 0; i < entries.Count && i < Rows; i++)
                    {
                        var e = entries[i];
                        Color c = e.IsYou ? Neon.Lime : Color.white;
                        rankT[i].text = "#" + e.Rank; nameT[i].text = e.IsYou ? e.Name + "  (you)" : e.Name; scoreT[i].text = e.Score.ToString("N0");
                        rankT[i].color = nameT[i].color = scoreT[i].color = c;
                    }
                },
                err => { if (gameObject.activeInHierarchy) status.text = "Could not load: " + err; });
        }

        void Clear()
        {
            for (int i = 0; i < Rows; i++) rankT[i].text = nameT[i].text = scoreT[i].text = "";
        }
    }
}
