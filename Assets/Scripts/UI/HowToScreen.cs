using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TiltRush
{
    /// <summary>
    /// "How to play" as big cards: one card fills the screen, swipe up/down to change cards
    /// (or use the arrow buttons / arrow keys / mouse wheel). Long cards scroll first; swiping past the end
    /// goes to the next card. Text is large, gadgets / enemies / hazards are shown with their pictures.
    /// </summary>
    public class HowToScreen : UIScreen
    {
        CardDeck deck;
        protected override bool HasVignette { get { return true; } }

        protected override void Build()
        {
            var title = UIKit.Title(transform, "HOW TO PLAY", 84);
            UIKit.Anchor(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -18), new Vector2(1200, 110));

            // card area: everything between the title and the BACK button
            var panel = UIKit.Panel(transform, -40, 130, 1640, 790);
            deck = CardDeck.Create(panel, new Vector2(1500, 740));

            BuildCards();

            // arrows on the right side (for PC and for people who do not swipe)
            var up = UIKit.MakeButton(transform, "", Neon.Blue, new Vector2(110, 110), () => deck.Go(-1), 40);
            UIKit.Anchor((RectTransform)up.transform, new Vector2(1f, 0.5f), new Vector2(-30, 140), new Vector2(110, 110));
            Arrow(up.transform, 0f);
            var down = UIKit.MakeButton(transform, "", Neon.Blue, new Vector2(110, 110), () => deck.Go(1), 40);
            UIKit.Anchor((RectTransform)down.transform, new Vector2(1f, 0.5f), new Vector2(-30, -140), new Vector2(110, 110));
            Arrow(down.transform, 180f);

            MenuUtil.AddButton(transform, "BACK", Neon.Gray, -230f, () => UI.Show<MainMenuScreen>(), 420f);
            MenuUtil.AddButton(transform, "PLAY TUTORIAL", Neon.Aqua, 230f, () => App.I.Session.StartTutorial(), 420f);
        }

        static void Arrow(Transform parent, float rot)
        {
            var i = UIKit.Icon(parent, SpriteFactory.Fill(ShapeType.Triangle), 56, Color.white);
            i.rectTransform.localRotation = Quaternion.Euler(0, 0, rot);
        }

        public override void OnShow() { deck.Show(0, false); }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.PageDown)) deck.Go(1);
            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.PageUp)) deck.Go(-1);
        }

        // ------------------------------------------------------------------ content
        void BuildCards()
        {
            var c = deck.NewCard();
            c.Heading("GOAL", Neon.Aqua);
            c.Para("Survive as long as you can! Spiky dots hunt you down - touch one and the round is over.");
            c.Heading("CONTROLS", Neon.Aqua);
            c.Para(Platform.IsPhone
                ? "Tilt your phone to move. Pick your favourite way to hold it in OPTIONS > CONTROLS > TILT SETUP."
                : Platform.MoveHint);
            c.Para(Platform.CoopHint);
            c.Hint("Swipe up for the next card");

            c = deck.NewCard();
            c.Heading("SCORE", Neon.Yellow);
            c.Para("You earn points for every second you survive - and more the harder it gets.");
            c.Para("Bonus points for every dot you destroy and every pickup you collect.");
            c.Para("Compare your CLASSIC score with other players on the LEADERBOARD.");

            var gadgets = GadgetLibrary.All;
            const int perCard = 5;
            int parts = (gadgets.Count + perCard - 1) / perCard;
            for (int part = 0; part < parts; part++)
            {
                c = deck.NewCard();
                c.Heading("PICKUPS " + (part + 1) + "/" + parts, Neon.Lime);
                if (part == 0) c.Para("Fly over a pickup to use it immediately.");
                for (int i = part * perCard; i < Mathf.Min(gadgets.Count, (part + 1) * perCard); i++)
                {
                    var g = gadgets[i];
                    c.Row(holder => GadgetToken(holder, g), g.Name.ToUpper(), g.Color, g.Description);
                }
            }

            c = deck.NewCard();
            c.Heading("ENEMIES", Neon.Red);
            c.Row(h => EnemyIcon(h, ShapeType.Saw, EnemyType.Normal, false), "SAW", GameTheme.EnemyRim(EnemyType.Normal), "The normal dot. Chases you at medium speed.");
            c.Row(h => EnemyIcon(h, ShapeType.Spike6, EnemyType.Fast, false), "SPIKE STAR", GameTheme.EnemyRim(EnemyType.Fast), "Small and fast - keep moving!");
            c.Row(h => EnemyIcon(h, ShapeType.Spike10, EnemyType.Big, true), "SPIKE BALL", GameTheme.EnemyRim(EnemyType.Big), "Big and slow, but hard to get around.");
            c.Para("New dots blink for a moment before they become dangerous.");

            c = deck.NewCard();
            c.Heading("HAZARDS", Neon.Orange);
            c.Row(h => Bar(h), "RED WALLS", new Color32(255, 90, 90, 255), "Bars sweep across the arena. Slip around their ends!");
            c.Row(h => Blade(h), "SWORDS", new Color32(255, 150, 150, 255), "A dark lane with light red edges warns you - then a sword races along it.");
            c.Row(h => RingIcon(h), "LAUNCH RINGS", Neon.Yellow, "Yellow rings fling you backwards - escape danger or get thrown into it!");
            c.Row(h => Block(h), "OBSTACLES", new Color32(160, 180, 220, 255), "Dark blocks (campaign) stop you and the dots, but never hurt. Hide behind them!");

            c = deck.NewCard();
            c.Heading("MODES", Neon.Pink);
            foreach (var m in Modes.All) c.Row(h => ModeIcon(h, m.Color), m.Name, m.Color, m.Description);

            c = deck.NewCard();
            c.Heading("SKINS & COLOURS", Neon.Purple);
            c.Para("Complete campaign levels to unlock new colours.");
            c.Para("New skins are unlocked by playing: high scores in Classic and Hard, long Dodge runs, destroyed dots, collected pickups and online wins.");
            c.Para("Open the WARDROBE and tap a locked skin to see what is still missing.");

            // optional: Assets/Resources/Music/credits.txt (one line per track)
            var credits = Resources.Load<TextAsset>("Music/credits");
            if (credits != null && credits.text.Trim().Length > 0)
            {
                c = deck.NewCard();
                c.Heading("MUSIC CREDITS", Neon.Aqua);
                foreach (var line in credits.text.Split('\n'))
                    if (line.Trim().Length > 0) c.Para(line.Trim());
            }

            deck.Finish();
        }

        // ------------------------------------------------------------------ pictures
        public static void GadgetToken(RectTransform h, GadgetDef g)
        {
            UIKit.Icon(h, SpriteFactory.Fill(ShapeType.Circle), 120, new Color(0, 0, 0, 0.75f));
            UIKit.Icon(h, SpriteFactory.Fill(ShapeType.Circle), 106, new Color(g.Color.r * 0.35f, g.Color.g * 0.35f, g.Color.b * 0.35f, 1f));
            UIKit.Icon(h, SpriteFactory.Outline(ShapeType.Circle), 106, g.Color);
            UIKit.Icon(h, SpriteFactory.Gadget(g.Icon), 62, Color.white);
        }

        static void EnemyIcon(RectTransform h, ShapeType shape, EnemyType type, bool eyes)
        {
            Color rim = GameTheme.EnemyRim(type);
            UIKit.Icon(h, SpriteFactory.Glow(), 150, new Color(rim.r, rim.g, rim.b, 0.4f));
            UIKit.Icon(h, SpriteFactory.Fill(shape), 110, GameTheme.EnemyBody);
            UIKit.Icon(h, SpriteFactory.Outline(shape), 110, rim);
            if (eyes) UIKit.Icon(h, SpriteFactory.AngryEyes(), 64, GameTheme.EnemyEye);
            else UIKit.Icon(h, SpriteFactory.Fill(ShapeType.Circle), 26, GameTheme.EnemyEye);
        }

        static void Bar(RectTransform h)
        {
            var g = UIKit.Icon(h, SpriteFactory.Glow(), 150, new Color(1f, 0.1f, 0.1f, 0.45f));
            g.rectTransform.sizeDelta = new Vector2(150, 70);
            var b = UIKit.Icon(h, SpriteFactory.Pixel(), 10, new Color32(255, 70, 70, 255)); b.rectTransform.sizeDelta = new Vector2(120, 30);
            var i = UIKit.Icon(h, SpriteFactory.Pixel(), 10, new Color32(60, 0, 12, 255)); i.rectTransform.sizeDelta = new Vector2(110, 20);
        }

        static void Blade(RectTransform h)
        {
            var lane = UIKit.Icon(h, SpriteFactory.Pixel(), 10, new Color(0.1f, 0f, 0.02f, 0.6f)); lane.rectTransform.sizeDelta = new Vector2(46, 130);
            var f = UIKit.Icon(h, SpriteFactory.BladeFill(), 120, new Color32(235, 235, 255, 255)); f.rectTransform.sizeDelta = new Vector2(30, 120);
            var l = UIKit.Icon(h, SpriteFactory.BladeLine(), 120, new Color32(255, 60, 60, 255)); l.rectTransform.sizeDelta = new Vector2(30, 120);
            f.rectTransform.localRotation = l.rectTransform.localRotation = lane.rectTransform.localRotation = Quaternion.Euler(0, 0, -35);
        }

        static void RingIcon(RectTransform h)
        {
            UIKit.Icon(h, SpriteFactory.Glow(), 150, new Color(1f, 0.9f, 0f, 0.35f));
            UIKit.Icon(h, SpriteFactory.Outline(ShapeType.Circle), 110, Neon.Yellow);
            UIKit.Icon(h, SpriteFactory.Fill(ShapeType.Circle), 34, Neon.Yellow);
        }

        static void Block(RectTransform h)
        {
            UIKit.Icon(h, SpriteFactory.TileFill(), 110, new Color32(160, 180, 220, 255));
            UIKit.Icon(h, SpriteFactory.TileFill(), 96, new Color32(18, 16, 30, 255));
            UIKit.Icon(h, SpriteFactory.TileOutline(), 96, new Color(0.63f, 0.7f, 0.86f, 0.5f));
        }

        static void ModeIcon(RectTransform h, Color c)
        {
            UIKit.Icon(h, SpriteFactory.TileFill(), 96, Theme.Outline);
            UIKit.Icon(h, SpriteFactory.TileFill(), 84, Theme.Tone(c));
            UIKit.Icon(h, SpriteFactory.Fill(ShapeType.Triangle), 44, Color.white).rectTransform.localRotation = Quaternion.Euler(0, 0, -90);
        }
    }

    /// <summary>
    /// A stack of full-size cards. Vertical drag scrolls the current card; dragging further than the end
    /// (or a quick swipe on a short card) switches to the next / previous card with a slide animation.
    /// </summary>
    public class CardDeck : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IScrollHandler
    {
        public class Card
        {
            public RectTransform Root, Content;
            public float Height;                // total height of the content
            readonly float width;

            public Card(RectTransform root, RectTransform content, float width) { Root = root; Content = content; this.width = width; }

            Text Text(string s, int size, Color c, float x, float w, TextAnchor a = TextAnchor.UpperLeft)
            {
                var t = UIKit.Label(Content, s, size, c, a);
                var rt = t.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 1f);
                rt.sizeDelta = new Vector2(w, 10f);
                float h = Mathf.Ceil(t.preferredHeight * 1.08f) + 6f;
                rt.sizeDelta = new Vector2(w, h);
                rt.anchoredPosition = new Vector2(x, -Height);
                return t;
            }

            public void Heading(string s, Color c) { Height += Height > 0 ? 24f : 0f; Text(s, 64, c, 0, width); Height += 76f + 8f; }

            public void Para(string s)
            {
                var t = Text(s, 42, Color.white, 0, width);
                Height += t.rectTransform.sizeDelta.y + 18f;
            }

            public void Hint(string s)
            {
                var t = Text(s, 32, Theme.TextDim, 0, width, TextAnchor.UpperCenter);
                Height += t.rectTransform.sizeDelta.y + 10f;
            }

            /// <summary>Picture on the left, coloured title + description on the right.</summary>
            public void Row(System.Action<RectTransform> picture, string title, Color color, string desc)
            {
                const float pic = 150f;
                float top = Height;
                var holder = UIKit.Rect("Pic", Content);
                holder.anchorMin = holder.anchorMax = new Vector2(0f, 1f);
                holder.pivot = new Vector2(0.5f, 0.5f);
                holder.sizeDelta = new Vector2(pic, pic);
                picture(holder);

                var tt = Text(title, 46, color, pic + 30f, width - pic - 30f);
                Height += tt.rectTransform.sizeDelta.y;
                var dt = Text(desc, 38, Color.white, pic + 30f, width - pic - 30f);
                Height += dt.rectTransform.sizeDelta.y;

                float rowH = Mathf.Max(pic, Height - top);
                holder.anchoredPosition = new Vector2(pic * 0.5f, -(top + rowH * 0.5f));
                Height = top + rowH + 22f;
            }
        }

        const float SwitchDistance = 140f;   // how far you have to pull past the end to change cards
        readonly List<Card> cards = new List<Card>();
        RectTransform viewport;
        Image[] dots;
        Vector2 size;
        int index, leaving = -1;
        float scroll, shownScroll, slide, leaveSlide;
        bool dragging;
        Canvas canvas;

        public static CardDeck Create(RectTransform parent, Vector2 size)
        {
            var vp = UIKit.Rect("Cards", parent);
            vp.anchorMin = vp.anchorMax = vp.pivot = new Vector2(0.5f, 0.5f);
            vp.sizeDelta = size;
            vp.gameObject.AddComponent<RectMask2D>();
            var hit = vp.gameObject.AddComponent<Image>();       // invisible, catches the drags
            hit.color = new Color(0, 0, 0, 0);
            var d = vp.gameObject.AddComponent<CardDeck>();
            d.viewport = vp; d.size = size;
            return d;
        }

        public Card NewCard()
        {
            var root = UIKit.Rect("Card", viewport);
            root.anchorMin = root.anchorMax = root.pivot = new Vector2(0.5f, 1f);
            root.sizeDelta = new Vector2(size.x, size.y);
            var content = UIKit.Rect("Content", root);
            content.anchorMin = content.anchorMax = content.pivot = new Vector2(0.5f, 1f);
            content.sizeDelta = new Vector2(size.x - 60f, 100f);
            var c = new Card(root, content, size.x - 60f);
            cards.Add(c);
            return c;
        }

        /// <summary>Call after all cards are built: creates the page dots.</summary>
        public void Finish()
        {
            var bar = UIKit.Rect("Dots", viewport.parent);
            bar.anchorMin = bar.anchorMax = bar.pivot = new Vector2(1f, 0.5f);
            bar.sizeDelta = new Vector2(30, cards.Count * 36f);
            bar.anchoredPosition = new Vector2(-6, 0);
            dots = new Image[cards.Count];
            for (int i = 0; i < cards.Count; i++)
            {
                var d = UIKit.Icon(bar, SpriteFactory.Fill(ShapeType.Circle), 20, Color.white);
                d.rectTransform.anchorMin = d.rectTransform.anchorMax = new Vector2(0.5f, 1f);
                d.rectTransform.anchoredPosition = new Vector2(0, -18f - i * 36f);
                dots[i] = d;
            }
            Show(0, false);
        }

        float MaxScroll(int i) { return Mathf.Max(0f, cards[i].Height + 20f - size.y); }

        public void Show(int i, bool animate, int dir = 1)
        {
            if (cards.Count == 0) return;
            i = Mathf.Clamp(i, 0, cards.Count - 1);
            if (animate && i != index) { leaving = index; leaveSlide = 0f; slide = dir * size.y; }
            else { leaving = -1; slide = 0f; }
            index = i;
            scroll = shownScroll = dir < 0 && animate ? MaxScroll(i) : 0f;   // going back: start at the end of the card
            for (int k = 0; k < cards.Count; k++) cards[k].Root.gameObject.SetActive(k == index || k == leaving);
            if (dots != null)
                for (int k = 0; k < dots.Length; k++)
                {
                    dots[k].color = k == index ? Color.white : new Color(1, 1, 1, 0.3f);
                    dots[k].rectTransform.localScale = Vector3.one * (k == index ? 1.3f : 1f);
                }
            Layout();
        }

        public void Go(int dir)
        {
            int target = index + dir;
            if (target < 0 || target >= cards.Count) { scroll = Mathf.Clamp(scroll, 0f, MaxScroll(index)); return; }
            Show(target, true, dir);
        }

        float Scale { get { if (canvas == null) canvas = GetComponentInParent<Canvas>(); return canvas != null ? canvas.scaleFactor : 1f; } }

        public void OnBeginDrag(PointerEventData e) { dragging = true; }

        public void OnDrag(PointerEventData e)
        {
            scroll += e.delta.y / Scale;          // finger moves up -> content moves up -> read further down
        }

        public void OnEndDrag(PointerEventData e)
        {
            dragging = false;
            float max = MaxScroll(index);
            if (scroll > max + SwitchDistance) Go(1);
            else if (scroll < -SwitchDistance) Go(-1);
            else scroll = Mathf.Clamp(scroll, 0f, max);
        }

        public void OnScroll(PointerEventData e)
        {
            float max = MaxScroll(index);
            if (e.scrollDelta.y < 0 && scroll >= max - 1f) { Go(1); return; }
            if (e.scrollDelta.y > 0 && scroll <= 1f) { Go(-1); return; }
            scroll = Mathf.Clamp(scroll - e.scrollDelta.y * 80f, 0f, max);
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime, k = 1f - Mathf.Exp(-14f * dt);
            float max = MaxScroll(index);
            // pulling past the ends feels like a rubber band
            float target = scroll;
            if (dragging)
            {
                if (scroll < 0f) target = scroll * 0.45f;
                else if (scroll > max) target = max + (scroll - max) * 0.45f;
            }
            shownScroll = Mathf.Lerp(shownScroll, target, dragging ? 1f : k);
            slide = Mathf.Lerp(slide, 0f, k);
            if (leaving >= 0)
            {
                leaveSlide = Mathf.Lerp(leaveSlide, -Mathf.Sign(slide) * size.y, k);
                if (Mathf.Abs(slide) < 2f) { cards[leaving].Root.gameObject.SetActive(false); leaving = -1; slide = 0f; }
            }
            Layout();
        }

        void Layout()
        {
            if (cards.Count == 0) return;
            var cur = cards[index];
            cur.Root.anchoredPosition = new Vector2(0, -slide);
            cur.Content.anchoredPosition = new Vector2(0, shownScroll - 10f);
            if (leaving >= 0) cards[leaving].Root.anchoredPosition = new Vector2(0, -leaveSlide);
        }
    }
}
