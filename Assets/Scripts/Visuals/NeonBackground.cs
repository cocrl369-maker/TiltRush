using UnityEngine;

namespace TiltRush
{
    /// <summary>
    /// Geometry-Dash style background: a colour gradient, two layers of big blocks drifting diagonally (parallax),
    /// and a few large floating outline shapes. The colours come from GameTheme: calm at the start, more colourful
    /// and brighter the harder it gets. The blocks pulse softly with the music and drift faster at high difficulty.
    /// </summary>
    public class NeonBackground : MonoBehaviour
    {
        public static float Intensity = 0.1f;   // 0..1, set by GameWorld (= difficulty)

        const float FarSize = 2.6f, NearSize = 4.4f;
        const int FarCols = 18, FarRows = 10, NearCols = 12, NearRows = 7, FloaterCount = 9;

        SpriteRenderer baseQuad, gradient;
        SpriteRenderer[] far, near, floaters;
        BgPattern pattern = BgPattern.Tiles;

        // floating particles (snow, embers, bubbles ...)
        const int AmbientCount = 46;
        SpriteRenderer[] amb;
        Vector2[] ambPos;
        float[] ambSeed;
        Ambient ambKind = Ambient.None;
        Vector2[] floatPos, floatVel;
        float[] floatSpin, floatSize;
        Vector2 scrollFar, scrollNear;
        float shown = 0.1f;
        Camera cam;

        void Start()
        {
            baseQuad = Make("Base", SpriteFactory.Pixel(), -120);
            gradient = Make("Gradient", SpriteFactory.VGradient(), -119);

            near = new SpriteRenderer[NearCols * NearRows];
            for (int i = 0; i < near.Length; i++) near[i] = Make("Block", SpriteFactory.TileFill(), -114);
            far = new SpriteRenderer[FarCols * FarRows];
            for (int i = 0; i < far.Length; i++) far[i] = Make("Tile", SpriteFactory.TileOutline(), -113);

            amb = new SpriteRenderer[AmbientCount];
            ambPos = new Vector2[AmbientCount];
            ambSeed = new float[AmbientCount];
            var ar = new System.Random(3);
            for (int i = 0; i < AmbientCount; i++)
            {
                amb[i] = Make("Ambient", SpriteFactory.Glow(), -108);
                ambPos[i] = new Vector2((float)ar.NextDouble() * 30f - 15f, (float)ar.NextDouble() * 16f - 8f);
                ambSeed[i] = (float)ar.NextDouble();
            }

            var rnd = new System.Random(7);   // fixed seed -> same arrangement every time
            ShapeType[] kinds = { ShapeType.Diamond, ShapeType.Square, ShapeType.Circle, ShapeType.Triangle };
            floaters = new SpriteRenderer[FloaterCount];
            floatPos = new Vector2[FloaterCount]; floatVel = new Vector2[FloaterCount];
            floatSpin = new float[FloaterCount]; floatSize = new float[FloaterCount];
            for (int i = 0; i < FloaterCount; i++)
            {
                floaters[i] = Make("Floater", SpriteFactory.Outline(kinds[i % kinds.Length]), -112);
                floatPos[i] = new Vector2((float)rnd.NextDouble() * 24f - 12f, (float)rnd.NextDouble() * 14f - 7f);
                float a = (float)rnd.NextDouble() * 6.28f;
                floatVel[i] = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (0.15f + 0.25f * (float)rnd.NextDouble());
                floatSpin[i] = ((float)rnd.NextDouble() * 2f - 1f) * 25f;
                floatSize[i] = 1.6f + 2.4f * (float)rnd.NextDouble();
            }
        }

        SpriteRenderer Make(string n, Sprite s, int order)
        {
            var go = new GameObject(n);
            go.transform.SetParent(transform, false);
            var r = go.AddComponent<SpriteRenderer>();
            r.sprite = s; r.sortingOrder = order;
            return r;
        }

        void Update()
        {
            if (cam == null) cam = Camera.main;
            if (cam == null || far == null) return;

            var save = SaveManager.Data;
            float motion = save.bgMotion == 0 ? 0f : (save.bgMotion == 1 ? 0.35f : 1f);   // Options > Graphics > Background
            float dt = Time.unscaledDeltaTime * motion, t = Time.unscaledTime * motion + 10f;

            // in the menus the colours slowly travel through the whole palette (like the GD menu)
            float target = GameWorld.Current != null ? Intensity : 0.3f + 0.28f * Mathf.Sin(Time.unscaledTime * 0.06f);
            shown = Mathf.Lerp(shown, target, 1f - Mathf.Exp(-1.5f * Time.unscaledDeltaTime));
            GameTheme.SetLevel(shown);
            var th = GameTheme.Now;
            float beat = (App.I != null && App.I.Audio != null) ? App.I.Audio.Beat * motion : 0f;
            float bright = Mathf.Clamp(save.bgBrightness, 0.4f, 1.3f);

            transform.position = new Vector3(cam.transform.position.x, cam.transform.position.y, 0f);
            float h = cam.orthographicSize * 2f, w = h * cam.aspect;

            baseQuad.transform.localScale = new Vector3(w + 2f, h + 2f, 1f);
            baseQuad.color = Bright(th.BgBottom, bright);
            gradient.transform.localScale = new Vector3(w + 2f, h + 2f, 1f);
            gradient.color = Bright(th.BgTop, bright);

            // the level theme decides the shape of the blocks and the floating particles
            var lt = GameTheme.Override;
            SetPattern(lt != null ? lt.Pattern : BgPattern.Tiles);
            UpdateAmbient(lt != null ? lt.Ambient : Ambient.Sparkles, w, h, dt, t, th);

            // diagonal drift, faster when it gets harder (near layer moves faster = depth)
            Vector2 dir = new Vector2(0.94f, 0.34f);
            float speed = 0.3f + 1.1f * shown;
            scrollFar += dir * speed * 0.45f * dt;
            scrollNear += dir * speed * dt;

            // far layer: outlined blocks, a soft light wave runs across them
            float farA = Mathf.Lerp(0.07f, 0.16f, shown) * (1f + 0.8f * beat);
            Grid(far, FarCols, FarRows, FarSize, scrollFar, (ix, iy, p) =>
            {
                float wave = 0.6f + 0.4f * Mathf.Sin(t * 1.4f - (p.x + p.y) * 0.25f);
                return new Color(th.Tile.r, th.Tile.g, th.Tile.b, farA * wave);
            });

            // near layer: big filled blocks, some darker, some lighter (checker-like pattern)
            Grid(near, NearCols, NearRows, NearSize, scrollNear, (ix, iy, p) =>
            {
                float hsh = Hash(ix, iy);
                if (hsh < 0.38f) return new Color(th.Dark.r, th.Dark.g, th.Dark.b, 0.22f + 0.1f * shown);
                if (hsh < 0.6f) return new Color(th.Tile.r, th.Tile.g, th.Tile.b, (0.035f + 0.05f * shown) * (1f + beat));
                return Color.clear;
            });

            // big floating outline shapes
            for (int i = 0; i < FloaterCount; i++)
            {
                floatPos[i] += floatVel[i] * (0.6f + shown) * dt;
                float hw = w * 0.5f + floatSize[i], hh = h * 0.5f + floatSize[i];
                if (floatPos[i].x > hw) floatPos[i].x -= 2f * hw; else if (floatPos[i].x < -hw) floatPos[i].x += 2f * hw;
                if (floatPos[i].y > hh) floatPos[i].y -= 2f * hh; else if (floatPos[i].y < -hh) floatPos[i].y += 2f * hh;
                var tr = floaters[i].transform;
                tr.localPosition = floatPos[i];
                tr.localRotation = Quaternion.Euler(0, 0, floatSpin[i] * t);
                tr.localScale = Vector3.one * floatSize[i] * (1f + 0.06f * beat);
                floaters[i].color = new Color(th.Tile.r, th.Tile.g, th.Tile.b, Mathf.Lerp(0.06f, 0.13f, shown));
            }
        }

        void SetPattern(BgPattern p)
        {
            if (p == pattern) return;
            pattern = p;
            Sprite farS, nearS;
            switch (p)
            {
                case BgPattern.Bricks:   farS = SpriteFactory.BrickOutline(); nearS = SpriteFactory.TileFill(); break;
                case BgPattern.Crystals: farS = SpriteFactory.Outline(ShapeType.Diamond); nearS = SpriteFactory.Fill(ShapeType.Diamond); break;
                case BgPattern.Bubbles:  farS = SpriteFactory.Outline(ShapeType.Circle); nearS = SpriteFactory.Fill(ShapeType.Circle); break;
                case BgPattern.Stars:    farS = SpriteFactory.Outline(ShapeType.Star); nearS = SpriteFactory.Glow(); break;
                default:                 farS = SpriteFactory.TileOutline(); nearS = SpriteFactory.TileFill(); break;
            }
            foreach (var r in far) r.sprite = farS;
            foreach (var r in near) r.sprite = nearS;
        }

        /// <summary>Snow falls, embers and bubbles rise, stars twinkle, rain streaks down ... (wraps around the screen).</summary>
        void UpdateAmbient(Ambient kind, float w, float h, float dt, float t, ThemeColors th)
        {
            if (SaveManager.Data.particles == 0) kind = Ambient.None;   // Options > Graphics > Particles OFF
            if (kind != ambKind)
            {
                ambKind = kind;
                Sprite s = kind == Ambient.Bubbles ? SpriteFactory.Outline(ShapeType.Circle)
                         : kind == Ambient.Rain ? SpriteFactory.Pixel()
                         : kind == Ambient.Sparkles || kind == Ambient.Stars ? SpriteFactory.Fill(ShapeType.Star)
                         : SpriteFactory.Glow();
                foreach (var r in amb) { r.sprite = s; r.enabled = kind != Ambient.None; }
            }
            if (kind == Ambient.None) return;

            float hw = w * 0.5f + 1f, hh = h * 0.5f + 1f;
            for (int i = 0; i < AmbientCount; i++)
            {
                float sd = ambSeed[i];
                Vector2 v; float size; Color c; float rot = 0f; Vector2 scale;
                switch (kind)
                {
                    case Ambient.Snow:
                        v = new Vector2(0.35f * Mathf.Sin(t * 0.8f + sd * 20f), -(0.6f + sd * 0.9f));
                        size = 0.12f + sd * 0.22f; c = new Color(1f, 1f, 1f, 0.75f); break;
                    case Ambient.Embers:
                        v = new Vector2(0.3f * Mathf.Sin(t * 1.3f + sd * 30f), 0.8f + sd * 1.6f);
                        size = 0.1f + sd * 0.2f;
                        c = Color.Lerp(new Color(1f, 0.85f, 0.3f), new Color(1f, 0.35f, 0.1f), sd);
                        c.a = 0.55f + 0.45f * Mathf.Sin(t * 9f + sd * 50f); break;
                    case Ambient.Bubbles:
                        v = new Vector2(0.25f * Mathf.Sin(t * 1.6f + sd * 25f), 0.5f + sd * 0.9f);
                        size = 0.12f + sd * 0.3f; c = new Color(th.Tile.r, th.Tile.g, th.Tile.b, 0.45f); break;
                    case Ambient.Rain:
                        v = new Vector2(-2.5f, -(9f + sd * 5f));
                        size = 0.04f; c = new Color(th.Tile.r, th.Tile.g, th.Tile.b, 0.35f);
                        rot = Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg; break;
                    case Ambient.Dust:
                        v = new Vector2(1.2f + sd * 1.5f, 0.2f * Mathf.Sin(t + sd * 10f));
                        size = 0.08f + sd * 0.15f; c = new Color(1f, 0.92f, 0.75f, 0.45f); break;
                    case Ambient.Stars:
                        v = new Vector2(-0.05f, 0f);
                        size = 0.06f + sd * 0.12f; c = new Color(1f, 1f, 1f, 0.35f + 0.65f * Mathf.Abs(Mathf.Sin(t * (0.6f + sd * 2f) + sd * 40f))); break;
                    default:   // sparkles
                        v = new Vector2(0.1f, 0.15f + sd * 0.2f);
                        size = 0.08f + sd * 0.14f;
                        c = new Color(th.Tile.r, th.Tile.g, th.Tile.b, 0.5f * Mathf.Max(0f, Mathf.Sin(t * (1f + sd * 2f) + sd * 60f)));
                        rot = t * 40f * (sd - 0.5f); break;
                }
                Vector2 p = ambPos[i] + v * dt;
                if (p.x > hw) p.x -= 2f * hw; else if (p.x < -hw) p.x += 2f * hw;
                if (p.y > hh) p.y -= 2f * hh; else if (p.y < -hh) p.y += 2f * hh;
                ambPos[i] = p;

                scale = kind == Ambient.Rain ? new Vector2(0.7f, size) : Vector2.one * (kind == Ambient.Embers || kind == Ambient.Snow || kind == Ambient.Dust ? size * 2.5f : size * 2f);
                var tr = amb[i].transform;
                tr.localPosition = p;
                tr.localRotation = Quaternion.Euler(0, 0, rot);
                tr.localScale = new Vector3(scale.x, scale.y, 1f);
                amb[i].color = c;
            }
        }

        static Color Bright(Color c, float k) { return new Color(Mathf.Clamp01(c.r * k), Mathf.Clamp01(c.g * k), Mathf.Clamp01(c.b * k), 1f); }

        delegate Color TileColor(int ix, int iy, Vector2 pos);

        /// <summary>Lays out a scrolling grid. Each tile keeps its "world index" while moving, so patterns stay stable.</summary>
        static void Grid(SpriteRenderer[] tiles, int cols, int rows, float size, Vector2 scroll, TileColor color)
        {
            int kx = Mathf.FloorToInt(scroll.x / size), ky = Mathf.FloorToInt(scroll.y / size);
            float fx = scroll.x - kx * size, fy = scroll.y - ky * size;
            for (int j = 0; j < rows; j++)
                for (int i = 0; i < cols; i++)
                {
                    var r = tiles[j * cols + i];
                    Vector2 p = new Vector2((i - cols * 0.5f) * size + fx, (j - rows * 0.5f) * size + fy);
                    r.transform.localPosition = p;
                    r.transform.localScale = Vector3.one * size;
                    r.color = color(i - kx, j - ky, p);
                }
        }

        static float Hash(int x, int y)
        {
            unchecked
            {
                int h = x * 374761393 + y * 668265263;
                h = (h ^ (h >> 13)) * 1274126177;
                return ((h ^ (h >> 16)) & 0xFFFF) / 65535f;
            }
        }
    }
}
