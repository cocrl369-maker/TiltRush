using UnityEngine;
using UnityEngine.UI;

namespace TiltRush
{
    /// <summary>
    /// Level select: 25 level tiles in the colours of their world (ice, lava, candy ...).
    /// Finished levels show a check mark, the next playable level glows, locked levels are dark.
    /// </summary>
    public class CampaignScreen : UIScreen
    {
        const int Columns = 9;
        Button[] btns;
        Image[] swatch;
        Text info;
        protected override bool HasVignette { get { return true; } }

        protected override void Build()
        {
            var title = UIKit.Title(transform, "CAMPAIGN", 90);
            UIKit.Anchor(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -20), new Vector2(1200, 115));
            UIKit.Panel(transform, 0, 140, 1840, 570);

            const float w = 186f, h = 160f, gap = 14f;
            int n = Campaign.Levels.Length;
            btns = new Button[n];
            swatch = new Image[n];
            for (int i = 0; i < n; i++)
            {
                int idx = i;
                int col = i % Columns, row = i / Columns;
                var b = UIKit.MakeButton(transform, "", Neon.Blue, new Vector2(w, h), () => Play(idx), 24);
                UIKit.Anchor((RectTransform)b.transform, new Vector2(0.5f, 1f),
                             new Vector2((col - (Columns - 1) / 2f) * (w + gap), -(170f + row * (h + gap))), new Vector2(w, h));
                // small colour strip of the level's world at the top of the tile
                var strip = UIKit.Rect("World", b.transform);
                strip.anchorMin = new Vector2(0.5f, 1f); strip.anchorMax = new Vector2(0.5f, 1f); strip.pivot = new Vector2(0.5f, 1f);
                strip.sizeDelta = new Vector2(w - 50f, 12f); strip.anchoredPosition = new Vector2(0, -18f);
                swatch[i] = strip.gameObject.AddComponent<Image>();
                swatch[i].raycastTarget = false;
                btns[i] = b;
            }

            info = UIKit.Label(transform, "", 34, Color.white);
            UIKit.Anchor(info.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -725), new Vector2(1700, 200));

            MenuUtil.AddBack(transform, () => UI.Show<ModeSelectScreen>());
        }

        void Play(int index)
        {
            if (index > SaveManager.Data.campaignDone) { ShowInfo(index); return; }   // locked: just show what it is
            App.I.Session.StartCampaign(index);
        }

        public override void OnShow()
        {
            int done = SaveManager.Data.campaignDone;
            for (int i = 0; i < btns.Length; i++)
            {
                var lv = Campaign.Levels[i];
                var label = btns[i].GetComponentInChildren<Text>();
                string head = "<size=56>" + (i + 1) + "</size>\n<size=19>" + lv.Name.ToUpper() + "</size>";
                Color world = Color.Lerp(lv.Theme.Accent, lv.Theme.Bottom, 0.35f);
                swatch[i].color = i <= done ? Color.Lerp(lv.Theme.Top, lv.Theme.Bottom, 0.3f) : new Color(1, 1, 1, 0.15f);
                if (i < done)
                {
                    label.text = head + "\n<size=18><color=#7DFF00>DONE</color></size>";
                    UIKit.SetButtonColor(btns[i], world);
                }
                else if (i == done)
                {
                    label.text = head + "\n<size=18>PLAY</size>";
                    UIKit.SetButtonColor(btns[i], Neon.Lime);
                }
                else
                {
                    label.text = head + "\n<size=18>LOCKED</size>";
                    UIKit.SetButtonColor(btns[i], Theme.Locked, true);
                }
            }
            ShowInfo(Mathf.Min(done, Campaign.Levels.Length - 1));
            if (done >= Campaign.Levels.Length) info.text = "Campaign complete - you beat all 25 levels. Legend!";
        }

        void ShowInfo(int i)
        {
            var lv = Campaign.Levels[i];
            string reward = Unlocks.RewardsFor(i + 1);
            info.text = "Level " + (i + 1) + " - " + lv.Name + "   <color=#" + ColorUtility.ToHtmlStringRGB(lv.Theme.Accent) + ">[" + lv.Theme.Name.ToUpper() + "]</color>" +
                        "\nSurvive " + lv.Duration.ToString("0") + " s  -  " + lv.Dots + " dots" +
                        (lv.Walls > 0 ? ", " + lv.Walls + " walls" : "") + (lv.Swords > 0 ? ", " + lv.Swords + " swords" : "") +
                        (lv.Obstacles.Length > 0 ? ", obstacles" : "") +
                        "\nCompleted: " + SaveManager.Data.campaignDone + " / " + Campaign.Levels.Length +
                        (reward.Length > 0 ? "   -   Reward: " + reward : "");
        }
    }
}
