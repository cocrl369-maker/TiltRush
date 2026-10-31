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

            float dt = Time.unscaledDeltaTime, t = Time.unscaledTime;
            shown = Mathf.Lerp(shown, Intensity, 1f - Mathf.Exp(-1.5f * dt));
            GameTheme.SetLevel(shown);
            var th = GameTheme.Now;
            float beat = (App.I != null && App.I.Audio != null) ? App.I.Audio.Beat : 0f;

            transform.position = new Vector3(cam.transform.position.x, cam.transform.position.y, 0f);
            float h = cam.orthographicSize * 2f, w = h * cam.aspect;

            baseQuad.transform.localScale = new Vector3(w + 2f, h + 2f, 1f);
            baseQuad.color = th.BgBottom;
            gradient.transform.localScale = new Vector3(w + 2f, h + 2f, 1f);
            gradient.color = th.BgTop;

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
