using UnityEngine;
using UnityEngine.UI;

namespace TiltRush
{
    /// <summary>Level select: finished levels, the next playable level, locked levels.</summary>
    public class CampaignScreen : UIScreen
    {
        const int Columns = 6;
        Button[] btns;
        Text info;
        protected override bool HasVignette { get { return true; } }

        protected override void Build()
        {
            new UIStack((RectTransform)transform, 30).Label("CAMPAIGN", 80, Color.white, 100);
            UIKit.Panel(transform, 0, 150, 1800, 500);

            const float w = 270f, h = 200f, gap = 24f;
            btns = new Button[Campaign.Levels.Length];
            for (int i = 0; i < btns.Length; i++)
            {
                int idx = i;
                int col = i % Columns, row = i / Columns;
                var b = UIKit.MakeButton(transform, "", Neon.Blue, new Vector2(w, h), () => Play(idx), 30);
                UIKit.Anchor((RectTransform)b.transform, new Vector2(0.5f, 1f),
                             new Vector2((col - (Columns - 1) / 2f) * (w + gap), -(175f + row * (h + gap))), new Vector2(w, h));
                btns[i] = b;
            }

            info = UIKit.Label(transform, "", 34, Color.white);
            UIKit.Anchor(info.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -690), new Vector2(1500, 220));

            MenuUtil.AddBack(transform, () => UI.Show<ModeSelectScreen>());
        }

        void Play(int index)
        {
            if (index > SaveManager.Data.campaignDone) return;   // locked
            App.I.Session.StartCampaign(index);
        }

        public override void OnShow()
        {
            int done = SaveManager.Data.campaignDone;
            for (int i = 0; i < btns.Length; i++)
            {
                var lv = Campaign.Levels[i];
                string head = "<size=62>" + (i + 1) + "</size>\n<size=26>" + lv.Name.ToUpper() + "</size>\n";
                var label = btns[i].GetComponentInChildren<Text>();
                if (i < done)
                {
                    label.text = head + "<size=22>DONE</size>";
                    UIKit.SetButtonColor(btns[i], Neon.Green);
                }
                else if (i == done)
                {
                    label.text = head + "<size=22>" + lv.Duration.ToString("0") + "s - " + lv.Dots + " dots</size>";
                    UIKit.SetButtonColor(btns[i], Neon.Orange);
                }
                else
                {
                    label.text = head + "<size=22>LOCKED</size>";
                    UIKit.SetButtonColor(btns[i], Theme.Locked, true);
                }
            }

            if (done >= Campaign.Levels.Length)
            {
                info.text = "Campaign complete - all rewards unlocked!";
                return;
            }
            var next = Campaign.Levels[done];
            string reward = Unlocks.RewardsFor(done + 1);
            info.text = "Completed: " + done + " / " + Campaign.Levels.Length +
                        "\nNext: level " + (done + 1) + " - " + next.Name + " (" + next.Dots + " dots" +
                        (next.Walls > 0 ? ", " + next.Walls + " walls" : "") + (next.Swords > 0 ? ", " + next.Swords + " swords" : "") +
                        (next.Obstacles.Length > 0 ? ", obstacles" : "") + ")" +
                        "\nReward: " + (reward.Length > 0 ? reward : "-");
        }
    }
}
