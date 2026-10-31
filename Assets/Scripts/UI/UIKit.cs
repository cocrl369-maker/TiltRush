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
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = SpriteFactory.RoundedFill(); img.type = Image.Type.Sliced;
            img.color = Theme.Panel; img.raycastTarget = false;

            var rim = Stretch(Rect("Rim", rt));
            var ri = rim.gameObject.AddComponent<Image>();
            ri.sprite = SpriteFactory.RoundedRingThick(); ri.type = Image.Type.Sliced;
            ri.color = Theme.PanelRim; ri.raycastTarget = false;

            var inner = Stretch(Rect("InnerRim", rt));              // thin light line just inside the dark rim
            inner.offsetMin = new Vector2(10, 10); inner.offsetMax = new Vector2(-10, -10);
            var ii = inner.gameObject.AddComponent<Image>();
            ii.sprite = SpriteFactory.RoundedRing(); ii.type = Image.Type.Sliced;
            ii.color = new Color(1f, 1f, 1f, 0.07f); ii.raycastTarget = false;
            return rt;
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
            b.GetComponent<Image>().color = raw ? c : Theme.Tone(c);
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

            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = SpriteFactory.RoundedFill();
            img.type = Image.Type.Sliced;
            img.color = raw ? color : Theme.Tone(color);

            var shadow = rt.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.45f);
            shadow.effectDistance = new Vector2(0, -9);

            var shade = Rect("Shade", rt);                       // darker lower half
            shade.anchorMin = Vector2.zero; shade.anchorMax = new Vector2(1, 0.5f);
            shade.offsetMin = new Vector2(8, 8); shade.offsetMax = new Vector2(-8, 0);
            var shadeImg = shade.gameObject.AddComponent<Image>();
            shadeImg.sprite = SpriteFactory.RoundedFill(); shadeImg.type = Image.Type.Sliced;
            shadeImg.color = new Color(0, 0, 0, 0.2f); shadeImg.raycastTarget = false;

            var gloss = Rect("Gloss", rt);                       // soft highlight on top
            gloss.anchorMin = new Vector2(0, 0.5f); gloss.anchorMax = Vector2.one;
            gloss.offsetMin = new Vector2(16, 0); gloss.offsetMax = new Vector2(-16, -10);
            var glossImg = gloss.gameObject.AddComponent<Image>();
            glossImg.sprite = SpriteFactory.RoundedFill(); glossImg.type = Image.Type.Sliced;
            glossImg.color = new Color(1, 1, 1, 0.16f); glossImg.raycastTarget = false;

            var light = Stretch(Rect("LightRim", rt));             // thin bright bevel inside
            light.offsetMin = new Vector2(9, 9); light.offsetMax = new Vector2(-9, -9);
            var lightImg = light.gameObject.AddComponent<Image>();
            lightImg.sprite = SpriteFactory.RoundedRing(); lightImg.type = Image.Type.Sliced;
            lightImg.color = new Color(1, 1, 1, 0.35f); lightImg.raycastTarget = false;

            var ring = Stretch(Rect("Rim", rt));                   // thick dark outline (GD look)
            var ringImg = ring.gameObject.AddComponent<Image>();
            ringImg.sprite = SpriteFactory.RoundedRingThick(); ringImg.type = Image.Type.Sliced;
            ringImg.color = Theme.Outline; ringImg.raycastTarget = false;

            var b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            b.transition = Selectable.Transition.None;               // ButtonFx does the animation
            b.navigation = new Navigation { mode = Navigation.Mode.None };

            var fx = rt.gameObject.AddComponent<ButtonFx>();
            fx.Pulse = pulse;

            b.onClick.AddListener(() =>
            {
                if (SaveManager.Data.buttonSounds) App.I.Audio.Play("click");
                if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);   // keys must not re-trigger buttons
            });
            if (onClick != null) b.onClick.AddListener(onClick);

            Stretch(Label(rt, text, fontSize, Color.white).rectTransform);
            return b;
        }
    }

    /// <summary>Press feedback: squash while pressed, spring back with overshoot, ring burst on click.</summary>
    public class ButtonFx : MonoBehaviour, IPointerDownHandler, IPointerUpHandler,
                            IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        public bool Pulse;          // gentle "breathing" (used for the big PLAY button)
        float scale = 1f, velocity;
        bool down, hover;

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
