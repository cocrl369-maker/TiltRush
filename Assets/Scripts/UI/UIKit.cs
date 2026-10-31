using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TiltRush
{
    /// <summary>
    /// Look of the UI (Geometry-Dash style): bright, slightly softened button colours with thick dark outlines,
    /// translucent dark panels so the colourful background shines through, white text with dark outlines.
    /// </summary>
    public static class Theme
    {
        public static readonly Color Panel = new Color(0.02f, 0.01f, 0.06f, 0.6f);
        public static readonly Color PanelRim = new Color(0f, 0f, 0f, 0.55f);
        public static readonly Color TextDim = new Color(1f, 1f, 1f, 0.8f);
        public static readonly Color Locked = new Color(0.16f, 0.15f, 0.22f, 1f);
        public static readonly Color Outline = new Color(0.03f, 0.02f, 0.06f, 0.9f);   // dark button outline

        /// <summary>Softens a bright colour a little (colourful, but not glaring).</summary>
        public static Color Tone(Color c)
        {
            float h, s, v;
            Color.RGBToHSV(c, out h, out s, out v);
            return Color.HSVToRGB(h, s * 0.88f, v * 0.92f);
        }
    }

    /// <summary>Small helpers to build uGUI from code.</summary>
    public static class UIKit
    {
        static Font font;
        public static Font Font
        {
            get
            {
                if (font == null)
#if UNITY_2022_2_OR_NEWER
                    font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
#else
                    font = Resources.GetBuiltinResource<Font>("Arial.ttf");
#endif
                return font;
            }
        }

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        public static RectTransform Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return rt;
        }

        public static void Anchor(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
            rt.anchoredPosition = pos; rt.sizeDelta = size;
        }

        public static Text Label(Transform parent, string text, int size, Color color,
                                 TextAnchor anchor = TextAnchor.MiddleCenter, bool outline = true)
        {
            var rt = Rect("Label", parent);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Font; t.text = text; t.fontSize = size; t.color = color;
            t.alignment = anchor; t.fontStyle = FontStyle.Bold;
            t.raycastTarget = false; t.supportRichText = true;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            if (outline)
            {
                float w = size >= 70 ? 4f : (size >= 40 ? 3f : 2f);   // bigger text -> thicker dark outline
                var o = rt.gameObject.AddComponent<Outline>();
                o.effectColor = new Color(0, 0, 0, 0.85f); o.effectDistance = new Vector2(w, -w);
            }
            return t;
        }

        /// <summary>
        /// Keeps a label inside its parent with a margin: long text wraps onto a new line and, if it still
        /// does not fit, gets smaller (never over the edge).
        /// </summary>
        public static void FitInside(Text t, float marginX, float marginY)
        {
            var rt = Stretch(t.rectTransform);
            rt.offsetMin = new Vector2(marginX, marginY); rt.offsetMax = new Vector2(-marginX, -marginY);
            ShrinkToFit(t);
        }

        /// <summary>Wrap + shrink (best fit) between 45 % and 100 % of the font size.</summary>
        public static void ShrinkToFit(Text t)
        {
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            t.resizeTextForBestFit = true;
            t.resizeTextMaxSize = t.fontSize;
            t.resizeTextMinSize = Mathf.Max(12, Mathf.RoundToInt(t.fontSize * 0.45f));
        }

        /// <summary>Big title: white text, very thick dark outline and a drop shadow (GD logo style).</summary>
        public static Text Title(Transform parent, string text, int size)
        {
            var t = Label(parent, text, size, Color.white);
            var o2 = t.gameObject.AddComponent<Outline>();
            o2.effectColor = new Color(0, 0, 0, 0.9f); o2.effectDistance = new Vector2(-6, 6);
            var sh = t.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(0, 0, 0, 0.6f); sh.effectDistance = new Vector2(0, -12);
            return t;
        }

        /// <summary>Plain image (icons), centred in the parent.</summary>
        public static Image Icon(Transform parent, Sprite sprite, float size, Color color)
        {
            var rt = Rect("Icon", parent);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(size, size);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite; img.color = color; img.raycastTarget = false;
            return img;
        }

        /// <summary>Dark rounded card with a thin light rim. Created BEFORE the content so it sits behind it.</summary>
        public static RectTransform Panel(Transform parent, float centerX, float topY, float w, float h)
        {
            var rt = Rect("Panel", parent);
            Anchor(rt, new Vector2(0.5f, 1f), new Vector2(centerX, -topY), new Vector2(w, h));
            var tint = rt.gameObject.AddComponent<PanelTint>();
            tint.Frame = Layer(rt, "Frame", SpriteFactory.RoundedFill(), 0f, 1.7f);
            tint.Fill = Layer(rt, "Fill", SpriteFactory.RoundedFill(), 5f, 1.7f);
            tint.Sheen = Layer(rt, "Sheen", SpriteFactory.RoundedSheen(), 5f, 1.7f);
            tint.Inner = Layer(rt, "InnerLine", SpriteFactory.RoundedRing(), 16f, 2.2f);
            tint.Border = Layer(rt, "Border", SpriteFactory.RoundedRing(), 4f, 1.4f);

            // futuristic corner brackets
            const float len = 46f, th = 6f, off = 12f;
            var brackets = new System.Collections.Generic.List<Image>();
            for (int cx = 0; cx < 2; cx++)
                for (int cy = 0; cy < 2; cy++)
                {
                    Vector2 corner = new Vector2(cx, cy);
                    Vector2 dir = new Vector2(cx == 0 ? 1 : -1, cy == 0 ? 1 : -1);
                    brackets.Add(Bar(rt, corner, dir * off, new Vector2(len, th), dir));
                    brackets.Add(Bar(rt, corner, dir * off, new Vector2(th, len), dir));
                }
            tint.Brackets = brackets.ToArray();
            return rt;
        }

        static Image Bar(RectTransform parent, Vector2 corner, Vector2 pos, Vector2 size, Vector2 dir)
        {
            var rt = Rect("Bracket", parent);
            rt.anchorMin = rt.anchorMax = corner;
            rt.pivot = new Vector2(dir.x > 0 ? 0f : 1f, dir.y > 0 ? 0f : 1f);
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            var img = rt.gameObject.AddComponent<Image>();
            img.raycastTarget = false;
            return img;
        }

        /// <summary>One stretched, 9-sliced image layer inside a button / panel. inset &gt; 0 = smaller, &lt; 0 = bigger.</summary>
        public static Image Layer(RectTransform parent, string name, Sprite sprite, float inset, float cornerSharpness)
        {
            var rt = Stretch(Rect(name, parent));
            rt.offsetMin = new Vector2(inset, inset); rt.offsetMax = new Vector2(-inset, -inset);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite; img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = cornerSharpness;     // > 1 = smaller corner radius (more "techy")
            img.raycastTarget = false;
            return img;
        }

        /// <summary>Darkened screen edges for depth.</summary>
        public static void AddVignette(Transform parent)
        {
            var rt = Stretch(Rect("Vignette", parent));
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = SpriteFactory.Vignette(); img.color = Color.black; img.raycastTarget = false;
        }

        public static void SetButtonColor(Button b, Color c, bool raw = false)
        {
            var fx = b.GetComponent<ButtonFx>();
            if (fx != null) fx.SetBase(c, raw);
            else b.GetComponent<Image>().color = raw ? c : Theme.Tone(c);
        }

        /// <summary>
        /// Button colour that fits the background: the button keeps its own colour (green = go, red = danger ...)
        /// but takes on a bit of the background's light; grey buttons take the background hue completely.
        /// Brightness and saturation stay high so buttons never disappear in the background.
        /// </summary>
        public static Color ThemedButton(Color c)
        {
            var th = GameTheme.Now;
            float h, s, v;
            Color.RGBToHSV(c, out h, out s, out v);
            if (s < 0.2f)                                                        // neutral (BACK, MENU ...)
                return Color.HSVToRGB(Mathf.Repeat(th.Hue + 0.03f, 1f), 0.45f, 0.62f);
            Color toned = Theme.Tone(c);
            Color mixed = Color.Lerp(toned, th.BgTop, 0.2f);
            Color.RGBToHSV(mixed, out h, out s, out v);
            return Color.HSVToRGB(h, Mathf.Max(s, 0.55f), Mathf.Max(v, 0.78f)); // never dull
        }

        /// <summary>
        /// Rounded, glossy console-style button: softened body colour, darker lower half, light rim,
        /// highlight on top, drop shadow. Squashes when pressed and springs back (ButtonFx), thump + ding sound.
        /// 'raw' = use the colour exactly as given (colour swatches).
        /// </summary>
        public static Button MakeButton(Transform parent, string text, Color color, Vector2 size,
                                        UnityAction onClick, int fontSize = 48, bool pulse = false, bool raw = false)
        {
            var rt = Rect("Button_" + text, parent);
            rt.sizeDelta = size;

            // invisible root graphic: catches the clicks
            var img = rt.gameObject.AddComponent<Image>();
            img.color = new Color(0, 0, 0, 0);

            // layered "futuristic" look, like the gadget tokens and the background blocks:
            // glow -> dark frame -> dark tinted inside with a light sheen -> thin inner line -> bright neon border
            var L = new ButtonLayers();
            L.Glow = Layer(rt, "Glow", SpriteFactory.RoundedGlow(), -22f, 1f);
            L.Frame = Layer(rt, "Frame", SpriteFactory.RoundedFill(), 0f, 1.7f);
            L.Fill = Layer(rt, "Fill", SpriteFactory.RoundedFill(), 6f, 1.7f);
            L.Sheen = Layer(rt, "Sheen", SpriteFactory.RoundedSheen(), 6f, 1.7f);
            L.Inner = Layer(rt, "InnerLine", SpriteFactory.RoundedRing(), 17f, 2.2f);
            L.Border = Layer(rt, "Border", SpriteFactory.RoundedRingThick(), 5f, 1.7f);

            var b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            b.transition = Selectable.Transition.None;               // ButtonFx does the animation
            b.navigation = new Navigation { mode = Navigation.Mode.None };

            var fx = rt.gameObject.AddComponent<ButtonFx>();
            fx.Pulse = pulse;
            fx.Setup(L, color, raw);

            b.onClick.AddListener(() =>
            {
                if (SaveManager.Data.buttonSounds) App.I.Audio.Play("click");
                if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);   // keys must not re-trigger buttons
            });
            if (onClick != null) b.onClick.AddListener(onClick);

            var label = Label(rt, text, fontSize, Color.white);
            FitInside(label, Mathf.Min(22f, size.x * 0.08f), Mathf.Min(12f, size.y * 0.1f));
            return b;
        }
    }

    /// <summary>The image layers of a button (see UIKit.MakeButton).</summary>
    public class ButtonLayers { public Image Glow, Frame, Fill, Sheen, Inner, Border; }

    /// <summary>Press feedback: squash while pressed, spring back with overshoot, ring burst on click.</summary>
    public class ButtonFx : MonoBehaviour, IPointerDownHandler, IPointerUpHandler,
                            IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        public bool Pulse;          // gentle "breathing" (used for the big PLAY button)
        float scale = 1f, velocity;
        bool down, hover;

        ButtonLayers layers;
        Color baseColor;
        bool raw;

        public void Setup(ButtonLayers l, Color c, bool raw)
        {
            layers = l;
            SetBase(c, raw);
        }

        public void SetBase(Color c, bool raw)
        {
            baseColor = c; this.raw = raw;
            ApplyColors();
        }

        static Color A(Color c, float a) { c.a = a; return c; }

        /// <summary>
        /// Colours of all layers. Normal buttons: bright neon border + glow in the button colour (tinted by the background),
        /// dark inside tinted with the same colour. 'raw' (colour swatches, locked): the inside shows the colour itself.
        /// </summary>
        void ApplyColors()
        {
            if (layers == null) return;
            var th = GameTheme.Now;
            float press = down ? 1f : (hover ? 0.4f : 0f);
            if (raw)
            {
                layers.Fill.color = Color.Lerp(baseColor, Color.white, 0.12f * press);
                layers.Border.color = Color.Lerp(baseColor, Color.white, 0.55f);
                layers.Inner.color = A(Color.white, 0.3f);
                layers.Sheen.color = A(Color.white, 0.18f);
                layers.Glow.color = A(baseColor, 0.25f);
            }
            else
            {
                Color c = UIKit.ThemedButton(baseColor);
                Color inside = Color.Lerp(c * 0.3f, th.Dark, 0.25f);
                layers.Fill.color = A(Color.Lerp(inside, c * 0.55f, press), 0.94f);
                layers.Border.color = Color.Lerp(c, Color.white, 0.15f + 0.25f * press);
                layers.Inner.color = A(c, 0.5f);
                layers.Sheen.color = A(c, 0.38f);
                layers.Glow.color = A(c, 0.32f + 0.25f * press);
            }
            layers.Frame.color = A(Color.Lerp(Theme.Outline, th.Dark, 0.4f), 0.95f);
        }

        void OnEnable() { scale = 1f; velocity = 0f; down = false; hover = false; transform.localScale = Vector3.one; }

        public void OnPointerDown(PointerEventData e) { down = true; if (SaveManager.Data.buttonSounds) App.I.Audio.Play("press"); }
        public void OnPointerUp(PointerEventData e) { down = false; }
        public void OnPointerEnter(PointerEventData e) { hover = true; }
        public void OnPointerExit(PointerEventData e) { hover = false; down = false; }
        public void OnPointerClick(PointerEventData e) { Burst(); }

        void Update()
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.033f);
            float goal = down ? 0.9f : (hover ? 1.04f : 1f);
            if (Pulse && !down) goal += 0.025f * Mathf.Sin(Time.unscaledTime * 3f);

            // damped spring -> squash, then a little bounce back
            ApplyColors();
            velocity += ((goal - scale) * 500f - velocity * 20f) * dt;
            scale += velocity * dt;
            transform.localScale = Vector3.one * scale;
        }

        void Burst()
        {
            var rt = UIKit.Stretch(UIKit.Rect("Burst", transform));
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = SpriteFactory.RoundedRing(); img.type = Image.Type.Sliced;
            img.raycastTarget = false;
            rt.gameObject.AddComponent<UIBurst>();
        }
    }

    /// <summary>Panels take a dark tint of the background colour, so they belong to the scene.</summary>
    public class PanelTint : MonoBehaviour
    {
        public Image Frame, Fill, Sheen, Inner, Border;
        public Image[] Brackets;

        void Update()
        {
            var th = GameTheme.Now;
            Frame.color = new Color(0f, 0f, 0f, 0.55f);
            Color f = Color.Lerp(Theme.Panel, th.Dark, 0.55f); f.a = 0.62f;
            Fill.color = f;
            Sheen.color = new Color(th.Tile.r, th.Tile.g, th.Tile.b, 0.06f);
            Inner.color = new Color(th.Tile.r, th.Tile.g, th.Tile.b, 0.12f);
            Border.color = new Color(th.Accent.r, th.Accent.g, th.Accent.b, 0.45f);
            Color br = Color.Lerp(th.Accent, Color.white, 0.3f);
            foreach (var b in Brackets) b.color = br;
        }
    }

    /// <summary>Expanding, fading ring (UI version of the in-game Fx).</summary>
    public class UIBurst : MonoBehaviour
    {
        float t;
        Image img;
        void Awake() { img = GetComponent<Image>(); }
        void Update()
        {
            t += Time.unscaledDeltaTime / 0.35f;
            transform.localScale = Vector3.one * Mathf.Lerp(1f, 1.3f, t);
            img.color = new Color(1, 1, 1, 0.9f * (1f - t));
            if (t >= 1f) Destroy(gameObject);
        }
    }

    /// <summary>UI version of the player (used in main menu + wardrobe). Reflects the saved style.</summary>
    public class PlayerPreview : MonoBehaviour
    {
        Image glow, backing, fill, line, detail;
        RectTransform spin;

        public static PlayerPreview Create(Transform parent)
        {
            var rt = UIKit.Rect("Preview", parent);
            var p = rt.gameObject.AddComponent<PlayerPreview>();
            p.glow = p.Img(rt, SpriteFactory.Glow(), 2.6f);
            p.spin = UIKit.Stretch(UIKit.Rect("Spin", rt));
            p.backing = p.Img(p.spin, SpriteFactory.Fill(ShapeType.Arrow), 1.3f);
            p.backing.color = new Color(0, 0, 0, 0.7f);
            p.fill = p.Img(p.spin, SpriteFactory.Fill(ShapeType.Arrow), 1f);
            p.detail = p.Img(p.spin, SpriteFactory.Fill(ShapeType.Arrow), 1f);
            p.line = p.Img(p.spin, SpriteFactory.Outline(ShapeType.Arrow), 1f);
            p.Refresh();
            return p;
        }

        Image Img(Transform parent, Sprite s, float scale)
        {
            var rt = UIKit.Stretch(UIKit.Rect("Img", parent));
            rt.localScale = Vector3.one * scale;
            var i = rt.gameObject.AddComponent<Image>();
            i.sprite = s; i.raycastTarget = false;
            return i;
        }

        public void Refresh()
        {
            Show(PlayerStyle.FromSave(0));
        }

        /// <summary>Shows any style (the wardrobe uses this to preview a skin before it is chosen).</summary>
        public void Show(PlayerStyle st)
        {
            backing.sprite = SpriteFactory.Fill(st.Shape);
            detail.sprite = st.Detail; detail.enabled = st.Detail != null; detail.color = st.Outline;
            fill.sprite = SpriteFactory.Fill(st.Shape);
            line.sprite = SpriteFactory.Outline(st.Shape);
            fill.color = st.Primary; line.color = st.Outline;
            glow.color = new Color(st.Outline.r, st.Outline.g, st.Outline.b, 0.25f);
        }

        void Update()
        {
            spin.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(Time.unscaledTime * 1.5f) * 35f);   // tip wobbles left/right
            transform.localScale = Vector3.one * (1f + 0.04f * Mathf.Sin(Time.unscaledTime * 3f));
        }
    }
}
