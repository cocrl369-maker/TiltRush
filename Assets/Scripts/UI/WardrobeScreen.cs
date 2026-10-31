using UnityEngine;
using UnityEngine.UI;

namespace TiltRush
{
    /// <summary>
    /// Wardrobe: big preview on the left, tabs SKINS / BODY / OUTLINE on the right.
    /// Skins are shown as icons in your colours; locked ones show what you have to do (tap them to see the progress).
    /// </summary>
    public class WardrobeScreen : UIScreen
    {
        const int Cols = 7;
        static readonly string[] TabNames = { "SKINS", "BODY COLOR", "OUTLINE" };

        PlayerPreview preview;
        Text nameText, infoText;
        RectTransform[] pages;
        Button[] tabs;
        int tab, focus = -1;                       // focus: skin shown on the left (also locked ones)

        Button[] skinBtns;
        Image[] skinFill, skinLine, skinDetail;
        Text[] skinLabel;
        Button[] bodyBtns, lineBtns;
        protected override bool HasVignette { get { return true; } }

        protected override void Build()
        {
            var title = UIKit.Title(transform, "WARDROBE", 96);
            UIKit.Anchor(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -25), new Vector2(1200, 120));

            // ---- left: preview + name + requirement
            UIKit.Panel(transform, -640, 160, 520, 770);
            var left = new UIStack((RectTransform)transform, 200, -640, 470);
            preview = PlayerPreview.Create(transform);
            left.Place((RectTransform)preview.transform, 300, 300);
            left.Space(20);
            nameText = left.Label("", 56, Color.white, 70);
            infoText = left.Label("", 30, Theme.TextDim, 220, TextAnchor.UpperCenter);

            // ---- right: tabs + pages
            UIKit.Panel(transform, 290, 160, 1140, 770);
            tabs = new Button[TabNames.Length];
            for (int i = 0; i < tabs.Length; i++)
            {
                int idx = i;
                tabs[i] = UIKit.MakeButton(transform, TabNames[i], Neon.Blue, new Vector2(340, 84), () => SetTab(idx), 34);
                UIKit.Anchor((RectTransform)tabs[i].transform, new Vector2(0.5f, 1f), new Vector2(290 + (i - 1) * 360, -190), new Vector2(340, 84));
            }
            pages = new RectTransform[3];
            for (int i = 0; i < 3; i++) pages[i] = UIKit.Stretch(UIKit.Rect("Page" + i, transform));

            BuildSkins(pages[0]);
            bodyBtns = Swatches(pages[1], idx => { if (Unlocks.ColorUnlocked(idx)) { SaveManager.Data.primary = idx; Refresh(); } });
            lineBtns = Swatches(pages[2], idx => { if (Unlocks.ColorUnlocked(idx)) { SaveManager.Data.outline = idx; Refresh(); } });

            MenuUtil.AddBack(transform, () => { SaveManager.Save(); UI.Show<MainMenuScreen>(); });
        }

        void BuildSkins(RectTransform page)
        {
            int n = Skins.All.Length;
            skinBtns = new Button[n]; skinFill = new Image[n]; skinLine = new Image[n]; skinDetail = new Image[n]; skinLabel = new Text[n];
            var st = new UIStack(page, 300, 290, 1060) { Spacing = 14f };
            for (int row = 0; row * Cols < n; row++)
            {
                int count = Mathf.Min(Cols, n - row * Cols);
                var items = new UIStack.Item[count];
                for (int c = 0; c < count; c++)
                {
                    int idx = row * Cols + c;
                    items[c] = new UIStack.Item("", Neon.Indigo, () => PickSkin(idx));
                }
                var btns = st.Row(140, 18, items);
                for (int c = 0; c < count; c++)
                {
                    int idx = row * Cols + c;
                    var b = btns[c];
                    skinBtns[idx] = b;
                    skinLabel[idx] = b.GetComponentInChildren<Text>();
                    skinLabel[idx].alignment = TextAnchor.LowerCenter;
                    skinLabel[idx].rectTransform.offsetMin = new Vector2(4, 10);
                    var holder = UIKit.Rect("IconHolder", b.transform);
                    holder.anchorMin = holder.anchorMax = new Vector2(0.5f, 1f); holder.pivot = new Vector2(0.5f, 1f);
                    holder.anchoredPosition = new Vector2(0, -12); holder.sizeDelta = new Vector2(90, 90);
                    var shape = Skins.All[idx].Shape;
                    skinFill[idx] = UIKit.Icon(holder, SpriteFactory.Fill(shape), 84, Color.white);
                    skinDetail[idx] = UIKit.Icon(holder, SpriteFactory.Detail(shape, Skins.All[idx].Detail), 84, Color.white);
                    skinDetail[idx].enabled = skinDetail[idx].sprite != null;
                    skinLine[idx] = UIKit.Icon(holder, SpriteFactory.Outline(shape), 84, Color.white);
                }
            }
        }

        void PickSkin(int idx)
        {
            focus = idx;
            if (Skins.All[idx].Unlocked) SaveManager.Data.playerShape = idx;
            Refresh();
        }

        /// <summary>4 rows x 5 colour swatches.</summary>
        static Button[] Swatches(RectTransform page, System.Action<int> onPick)
        {
            int n = Config.Palette.Length, perRow = 5;
            var all = new Button[n];
            var st = new UIStack(page, 310, 290, 1000) { Spacing = 18f, RawColors = true };
            for (int row = 0; row * perRow < n; row++)
            {
                int count = Mathf.Min(perRow, n - row * perRow);
                var items = new UIStack.Item[count];
                for (int c = 0; c < count; c++)
                {
                    int idx = row * perRow + c;
                    items[c] = new UIStack.Item("", Config.Palette[idx], () => onPick(idx));
                }
                var btns = st.Row(130, 30, items);
                for (int c = 0; c < count; c++) all[row * perRow + c] = btns[c];
            }
            return all;
        }

        void SetTab(int i)
        {
            tab = i;
            for (int k = 0; k < pages.Length; k++)
            {
                pages[k].gameObject.SetActive(k == i);
                UIKit.SetButtonColor(tabs[k], k == i ? Neon.Blue : Theme.Locked, k != i);
            }
            Refresh();
        }

        void Refresh()
        {
            var d = SaveManager.Data;
            var style = PlayerStyle.FromSave(0);

            for (int i = 0; i < skinBtns.Length; i++)
            {
                var skin = Skins.All[i];
                bool open = skin.Unlocked, chosen = i == d.playerShape && open;
                skinFill[i].color = open ? style.Primary : new Color(0, 0, 0, 0.55f);
                skinLine[i].color = open ? style.Outline : new Color(1, 1, 1, 0.25f);
                skinDetail[i].color = style.Outline;
                skinDetail[i].enabled = open && skinDetail[i].sprite != null;
                skinLabel[i].text = open ? skin.Name : skin.ShortRequirement;
                skinLabel[i].color = open ? Color.white : new Color(1f, 0.85f, 0.5f, 1f);
                UIKit.SetButtonColor(skinBtns[i], chosen ? Neon.Lime : (open ? Neon.Indigo : Theme.Locked), !open);
            }
            for (int i = 0; i < bodyBtns.Length; i++)
            {
                bool open = Unlocks.ColorUnlocked(i);
                string locked = "LV " + Unlocks.ColorLevel[i];
                bodyBtns[i].GetComponentInChildren<Text>().text = open ? (i == d.primary ? "OK" : "") : locked;
                lineBtns[i].GetComponentInChildren<Text>().text = open ? (i == d.outline ? "OK" : "") : locked;
                UIKit.SetButtonColor(bodyBtns[i], open ? Config.Palette[i] : Theme.Locked, true);
                UIKit.SetButtonColor(lineBtns[i], open ? Config.Palette[i] : Theme.Locked, true);
            }

            // left side: the focused skin (may be locked - then it is shown as a preview with the requirement)
            int f = focus >= 0 ? focus : Mathf.Abs(d.playerShape) % Skins.All.Length;
            var fs = Skins.All[f];
            var shown = style;
            shown.Shape = fs.Shape;
            shown.Detail = SpriteFactory.Detail(fs.Shape, fs.Detail);
            preview.Show(shown);
            nameText.text = fs.Name;
            infoText.text = fs.Unlocked
                ? (f == d.playerShape ? "<color=#7DFF00>SELECTED</color>" : "Tap again to select")
                : "<color=#FFD080>LOCKED</color>\n" + fs.Requirement;
            if (tab != 0) infoText.text = "Skins unlocked: " + Skins.UnlockedCount + " / " + Skins.All.Length + "\nColours unlock in the campaign.";
        }

        public override void OnShow() { focus = -1; SetTab(tab); }
    }
}
