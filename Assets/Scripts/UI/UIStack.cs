using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace TiltRush
{
    /// <summary>Vertical layout helper: add elements top to bottom inside a column (centerX / width).</summary>
    public class UIStack
    {
        public struct Item
        {
            public string Text; public Color Color; public UnityAction Action;
            public Item(string t, Color c, UnityAction a) { Text = t; Color = c; Action = a; }
        }

        public float Spacing = 24f;
        public bool RawColors;      // true: buttons in Row() use their colour exactly (colour swatches)
        readonly RectTransform root;
        readonly float cx, width;
        float y;

        public UIStack(RectTransform root, float startY, float centerX = 0f, float width = 1100f)
        {
            this.root = root; y = startY; cx = centerX; this.width = width;
        }

        public void Space(float px) { y += px; }

        public RectTransform Place(RectTransform rt, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(cx, -y);
            y += h + Spacing;
            return rt;
        }

        public Text Label(string text, int size, Color color, float height, TextAnchor anchor = TextAnchor.MiddleCenter)
        {
            var l = UIKit.Label(root, text, size, color, anchor);
            Place(l.rectTransform, width, height);
            return l;
        }

        public Button Button(string text, Color color, float w, float h, UnityAction action, int fontSize = 48, bool pulse = false)
        {
            var b = UIKit.MakeButton(root, text, color, new Vector2(w, h), action, fontSize, pulse);
            Place((RectTransform)b.transform, w, h);
            return b;
        }

        /// <summary>Several buttons side by side.</summary>
        public Button[] Row(float h, int fontSize, params Item[] items)
        {
            int n = items.Length;
            float w = (width - (n - 1) * Spacing) / n;
            var res = new Button[n];
            for (int i = 0; i < n; i++)
            {
                var b = UIKit.MakeButton(root, items[i].Text, items[i].Color, new Vector2(w, h), items[i].Action, fontSize, false, RawColors);
                var rt = (RectTransform)b.transform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2(cx - width / 2f + w / 2f + i * (w + Spacing), -y);
                res[i] = b;
            }
            y += h + Spacing;
            return res;
        }

        // ------------------------------------------------------------------ settings rows (label left, control right)

        /// <summary>Label on the left, a button on the right that cycles through the options. Returns a refresh action.</summary>
        public System.Action Choice(string label, string[] options, System.Func<int> get, System.Action<int> set, float h = 84f)
        {
            RowLabel(label, h);
            float cw = width * 0.46f;
            Button b = null;
            b = UIKit.MakeButton(root, "", Neon.Indigo, new Vector2(cw, h), () =>
            {
                set((get() + 1) % options.Length);
                if (onChanged != null) onChanged();
            }, 34);
            PlaceRight((RectTransform)b.transform, cw, h);
            var txt = b.GetComponentInChildren<Text>();
            System.Action refresh = () => { txt.text = options[Mathf.Clamp(get(), 0, options.Length - 1)]; };
            refresh();
            y += h + Spacing;
            return refresh;
        }

        /// <summary>ON / OFF switch (green when on).</summary>
        public System.Action Toggle(string label, System.Func<bool> get, System.Action<bool> set, float h = 84f)
        {
            RowLabel(label, h);
            float cw = width * 0.46f;
            Button b = null;
            b = UIKit.MakeButton(root, "", Neon.Gray, new Vector2(cw, h), () =>
            {
                set(!get());
                if (onChanged != null) onChanged();
            }, 34);
            PlaceRight((RectTransform)b.transform, cw, h);
            var txt = b.GetComponentInChildren<Text>();
            System.Action refresh = () =>
            {
                bool on = get();
                txt.text = on ? "ON" : "OFF";
                UIKit.SetButtonColor(b, on ? Neon.Green : Neon.Gray);
            };
            refresh();
            y += h + Spacing;
            return refresh;
        }

        /// <summary>Slider on the right side. Returns a refresh action (reads the value again).</summary>
        public System.Action SliderRow(string label, float min, float max, System.Func<float> get, System.Action<float> set, float h = 84f)
        {
            RowLabel(label, h);
            float cw = width * 0.46f;
            var s = BuildSlider(cw, 60f, min, max, get(), v => set(v));
            PlaceRight((RectTransform)s.transform, cw, 60f, (h - 60f) * 0.5f);
            y += h + Spacing;
            return () => s.SetValueWithoutNotify(get());
        }

        /// <summary>Called after any Choice/Toggle changed (e.g. to refresh dependent rows).</summary>
        public System.Action onChanged;

        void RowLabel(string text, float h)
        {
            var l = UIKit.Label(root, text, 34, Color.white, TextAnchor.MiddleLeft);
            var rt = l.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(width * 0.52f, h);
            rt.anchoredPosition = new Vector2(cx - width / 2f, -y);
        }

        void PlaceRight(RectTransform rt, float w, float h, float dy = 0f)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(cx + width / 2f, -(y + dy));
        }

        /// <summary>Single-line text input (e.g. the leaderboard name).</summary>
        public InputField TextField(string label, string value, int maxLength, UnityAction<string> onEndEdit)
        {
            Label(label, 38, Color.white, 50);
            var rt = UIKit.Rect("TextField", root);
            Place(rt, width, 84);
            var bg = rt.gameObject.AddComponent<Image>();
            bg.color = new Color(1, 1, 1, 0.14f);

            var text = UIKit.Label(rt, "", 44, Color.white, TextAnchor.MiddleLeft, false);
            UIKit.Stretch(text.rectTransform); text.rectTransform.offsetMin = new Vector2(20, 0); text.rectTransform.offsetMax = new Vector2(-20, 0);
            var hint = UIKit.Label(rt, "Enter name", 44, new Color(1, 1, 1, 0.4f), TextAnchor.MiddleLeft, false);
            UIKit.Stretch(hint.rectTransform); hint.rectTransform.offsetMin = new Vector2(20, 0); hint.rectTransform.offsetMax = new Vector2(-20, 0);

            var f = rt.gameObject.AddComponent<InputField>();
            f.targetGraphic = bg; f.textComponent = text; f.placeholder = hint;
            f.characterLimit = maxLength;
            f.text = value;
            f.onEndEdit.AddListener(onEndEdit);
            return f;
        }

        public Slider Slider(string label, float min, float max, float value, UnityAction<float> onChange)
        {
            Label(label, 38, Color.white, 50);
            var s = BuildSlider(width, 60, min, max, value, onChange);
            Place((RectTransform)s.transform, width, 60);
            return s;
        }

        Slider BuildSlider(float w, float h, float min, float max, float value, UnityAction<float> onChange)
        {
            var rt = UIKit.Rect("Slider", root);
            rt.sizeDelta = new Vector2(w, h);

            var bg = UIKit.Rect("BG", rt);
            bg.anchorMin = new Vector2(0, 0.3f); bg.anchorMax = new Vector2(1, 0.7f);
            bg.offsetMin = bg.offsetMax = Vector2.zero;
            var bgImg = bg.gameObject.AddComponent<Image>();
            bgImg.sprite = SpriteFactory.RoundedFill(); bgImg.type = Image.Type.Sliced; bgImg.pixelsPerUnitMultiplier = 4f;
            bgImg.color = new Color(0, 0, 0, 0.45f);

            var fillArea = UIKit.Rect("FillArea", rt);
            fillArea.anchorMin = new Vector2(0, 0.35f); fillArea.anchorMax = new Vector2(1, 0.65f);
            fillArea.offsetMin = fillArea.offsetMax = Vector2.zero;
            var fill = UIKit.Rect("Fill", fillArea);
            fill.sizeDelta = Vector2.zero;
            var fillImg = fill.gameObject.AddComponent<Image>();
            fillImg.sprite = SpriteFactory.RoundedFill(); fillImg.type = Image.Type.Sliced; fillImg.pixelsPerUnitMultiplier = 4f;
            fillImg.color = Theme.Tone(Neon.Lime);

            var handleArea = UIKit.Rect("HandleArea", rt);
            UIKit.Stretch(handleArea);
            handleArea.offsetMin = new Vector2(30, 0); handleArea.offsetMax = new Vector2(-30, 0);
            var handle = UIKit.Rect("Handle", handleArea);
            handle.sizeDelta = new Vector2(56, 56);
            var hImg = handle.gameObject.AddComponent<Image>();
            hImg.sprite = SpriteFactory.Fill(ShapeType.Circle); hImg.color = Color.white;
            var hOutline = handle.gameObject.AddComponent<Outline>();
            hOutline.effectColor = Theme.Outline; hOutline.effectDistance = new Vector2(3, -3);

            var s = rt.gameObject.AddComponent<Slider>();
            s.fillRect = fill; s.handleRect = handle; s.targetGraphic = hImg;
            s.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
            s.minValue = min; s.maxValue = max; s.value = value;
            s.onValueChanged.AddListener(onChange);
            return s;
        }
    }
}
