using UnityEngine;
using UnityEngine.UI;

namespace TiltRush
{
    /// <summary>Leaderboards with tabs (Classic, Hard, Dodge, Asteroids, Online-ELO): top 10 plus your own rank.</summary>
    public class LeaderboardScreen : UIScreen
    {
        const int Rows = 11;   // 10 + one extra row for "you"
        Text[] rankT = new Text[Rows], nameT = new Text[Rows], scoreT = new Text[Rows];
        Text status, title;
        Button[] tabs;
        string board = "classic";
        int request;
        protected override bool HasVignette { get { return true; } }

        /// <summary>Opens the screen on a specific board (e.g. right after a run in that mode).</summary>
        public static void Open(string boardId)
        {
            var s = App.I.UI.Get<LeaderboardScreen>();
            if (boardId != null) s.board = boardId;
            App.I.UI.Show<LeaderboardScreen>();
        }

        protected override void Build()
        {
            title = UIKit.Title(transform, "LEADERBOARD", 84);
            UIKit.Anchor(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -15), new Vector2(1400, 105));

            tabs = new Button[Boards.All.Length];
            const float tw = 300f, gap = 16f;
            for (int i = 0; i < tabs.Length; i++)
            {
                var b = Boards.All[i];
                tabs[i] = UIKit.MakeButton(transform, b.Title, b.Color, new Vector2(tw, 80), () => { board = b.Id; OnShow(); }, 32);
                UIKit.Anchor((RectTransform)tabs[i].transform, new Vector2(0.5f, 1f), new Vector2((i - (tabs.Length - 1) / 2f) * (tw + gap), -125), new Vector2(tw, 80));
            }

            UIKit.Panel(transform, 0, 222, 1240, 712);
            status = UIKit.Label(transform, "", 32, new Color(1, 1, 1, 0.8f));
            UIKit.Anchor(status.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -232), new Vector2(1100, 46));

            for (int i = 0; i < Rows; i++)
            {
                float y = -282f - i * 56f - (i == Rows - 1 ? 16f : 0f);
                rankT[i] = Cell(-430f, y, 150f, TextAnchor.MiddleRight);
                nameT[i] = Cell(-250f, y, 600f, TextAnchor.MiddleLeft);
                scoreT[i] = Cell(400f, y, 260f, TextAnchor.MiddleRight);
            }
            MenuUtil.AddBack(transform, () => UI.Show<ModeSelectScreen>());
        }

        Text Cell(float x, float y, float w, TextAnchor align)
        {
            var t = UIKit.Label(transform, "", 36, Color.white, align);
            var rt = t.rectTransform;
            UIKit.Anchor(rt, new Vector2(0.5f, 1f), new Vector2(x, y), new Vector2(w, 52));
            rt.pivot = new Vector2(align == TextAnchor.MiddleLeft ? 0f : 1f, 1f);
            rt.anchoredPosition = new Vector2(align == TextAnchor.MiddleLeft ? x : x + w / 2f, y);
            UIKit.ShrinkToFit(t);
            return t;
        }

        public override void OnShow()
        {
            var info = Boards.Get(board);
            for (int i = 0; i < tabs.Length; i++)
            {
                bool on = Boards.All[i].Id == board;
                UIKit.SetButtonColor(tabs[i], on ? Boards.All[i].Color : Theme.Locked, !on);
            }
            title.text = info.Title + (info.Elo ? " - ELO RANKING" : " LEADERBOARD");
            Clear();
            status.text = "Loading...";
            int req = ++request;
            string id = board;
            App.I.Online.GetLeaderboard(id, 10,
                entries =>
                {
                    if (!gameObject.activeInHierarchy || req != request) return;
                    status.text = Boards.MyBest(id) > 0 ? "" : (info.Elo ? "Play an online match to get ranked!" : "Play a " + info.Title + " run to get on the board!");
                    for (int i = 0; i < entries.Count && i < Rows; i++)
                    {
                        var e = entries[i];
                        Color c = e.IsYou ? Neon.Lime : Color.white;
                        rankT[i].text = "#" + e.Rank;
                        nameT[i].text = e.IsYou ? e.Name + "  (you)" : e.Name;
                        scoreT[i].text = info.Elo ? "ELO " + e.Score : e.Score.ToString("N0");
                        rankT[i].color = nameT[i].color = scoreT[i].color = c;
                    }
                },
                err => { if (gameObject.activeInHierarchy && req == request) status.text = "Could not load: " + err; });
        }

        void Clear()
        {
            for (int i = 0; i < Rows; i++) rankT[i].text = nameT[i].text = scoreT[i].text = "";
        }
    }
}
