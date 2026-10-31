using UnityEngine;

namespace TiltRush
{
    /// <summary>
    /// Permanent obstacle (campaign). It blocks the player AND the dots but never hurts.
    /// Hide behind it - dots slide around it. Its look comes from the level theme (GameTheme.Block):
    /// lava bricks, ice with shine, slime with bubbles, metal with rivets, candy stripes ...
    /// </summary>
    public class Obstacle : MonoBehaviour
    {
        bool circle;
        Vector2 center, half;
        float radius;

        public static Obstacle Create(Transform parent, ObstacleDef def, Vector2 arena)
        {
            var go = new GameObject("Obstacle");
            go.transform.SetParent(parent, false);
            var o = go.AddComponent<Obstacle>();
            o.center = new Vector2(def.X * arena.x, def.Y * arena.y);
            go.transform.position = o.center;

            var st = GameTheme.Block;
            if (def.Circle)
            {
                o.circle = true; o.radius = def.A;
                o.BuildCircle(st, def.A);
            }
            else
            {
                o.half = new Vector2(def.A * 0.5f, def.B * 0.5f);
                o.BuildRect(st, def.A, def.B);
            }
            return o;
        }

        // ------------------------------------------------------------------ looks
        const int Base = -24;   // sorting orders -24 .. -14 (below everything that moves)

        void BuildRect(BlockStyle st, float w, float h)
        {
            Color glow = st.Edge; glow.a = st.Glow;
            Quad(SpriteFactory.Glow(), Vector2.zero, new Vector2(w + 1.6f, h + 1.6f), 0f, glow, Base);
            Quad(SpriteFactory.Pixel(), Vector2.zero, new Vector2(w + 0.1f, h + 0.1f), 0f, new Color(0, 0, 0, 0.8f), Base + 1);   // dark outer line
            Quad(SpriteFactory.Pixel(), Vector2.zero, new Vector2(w, h), 0f, st.Edge, Base + 2);
            Quad(SpriteFactory.Pixel(), Vector2.zero, new Vector2(w - 0.16f, h - 0.16f), 0f, st.Fill, Base + 3);

            float iw = w - 0.16f, ih = h - 0.16f;
            Color d = st.Detail;
            switch (st.Pattern)
            {
                case BlockPattern.Bricks:
                    for (float y = -ih / 2 + 0.3f; y < ih / 2 - 0.05f; y += 0.3f)
                        Quad(SpriteFactory.Pixel(), new Vector2(0, y), new Vector2(iw, 0.05f), 0f, d, Base + 4);
                    int row = 0;
                    for (float y = -ih / 2; y < ih / 2 - 0.05f; y += 0.3f, row++)
                    {
                        float top = Mathf.Min(y + 0.3f, ih / 2), mid = (y + top) * 0.5f;
                        for (float x = -iw / 2 + (row % 2 == 0 ? 0.3f : 0.6f); x < iw / 2 - 0.05f; x += 0.6f)
                            Quad(SpriteFactory.Pixel(), new Vector2(x, mid), new Vector2(0.05f, top - y), 0f, d, Base + 4);
                    }
                    // glowing hot top edge
                    Quad(SpriteFactory.Pixel(), new Vector2(0, h / 2 - 0.05f), new Vector2(w, 0.1f), 0f, Color.Lerp(st.Edge, Color.white, 0.5f), Base + 5);
                    break;
                case BlockPattern.Shine:   // ice: two diagonal shine bars in the top-left corner
                {
                    float s = Mathf.Min(iw, ih);
                    Vector2 c0 = new Vector2(-iw / 2 + s * 0.3f, ih / 2 - s * 0.3f);
                    Quad(SpriteFactory.Pixel(), c0, new Vector2(s * 0.42f, s * 0.07f), 45f, d, Base + 4);
                    Quad(SpriteFactory.Pixel(), c0 + new Vector2(s * 0.12f, -s * 0.12f), new Vector2(s * 0.24f, s * 0.05f), 45f, d, Base + 4);
                    Frame(iw - 0.14f, ih - 0.14f, 0.03f, new Color(1, 1, 1, 0.35f), Base + 4);
                    break;
                }
                case BlockPattern.Dots:
                    for (float x = -iw / 2 + 0.25f; x < iw / 2 - 0.1f; x += 0.45f)
                        for (float y = -ih / 2 + 0.25f; y < ih / 2 - 0.1f; y += 0.45f)
                            Quad(SpriteFactory.Fill(ShapeType.Circle), new Vector2(x, y + 0.08f * Mathf.Sin(x * 7f)), Vector2.one * 0.16f, 0f, d, Base + 4);
                    break;
                case BlockPattern.Rivets:
                    Frame(iw - 0.16f, ih - 0.16f, 0.04f, d, Base + 4);
                    for (int cx = -1; cx <= 1; cx += 2)
                        for (int cy = -1; cy <= 1; cy += 2)
                            Quad(SpriteFactory.Fill(ShapeType.Circle), new Vector2(cx * (iw / 2 - 0.16f), cy * (ih / 2 - 0.16f)), Vector2.one * 0.13f, 0f, st.Edge, Base + 5);
                    break;
                case BlockPattern.Stripes:  // candy: stripes across the long side
                    if (iw >= ih)
                        for (float x = -iw / 2 + 0.2f; x < iw / 2; x += 0.42f)
                            Quad(SpriteFactory.Pixel(), new Vector2(x, 0), new Vector2(0.16f, ih), 0f, d, Base + 4);
                    else
                        for (float y = -ih / 2 + 0.2f; y < ih / 2; y += 0.42f)
                            Quad(SpriteFactory.Pixel(), new Vector2(0, y), new Vector2(iw, 0.16f), 0f, d, Base + 4);
                    break;
                case BlockPattern.Lines:    // sandstone layers
                    for (float y = -ih / 2 + 0.2f; y < ih / 2 - 0.05f; y += 0.22f)
                        Quad(SpriteFactory.Pixel(), new Vector2(0, y), new Vector2(iw, 0.04f), 0f, d, Base + 4);
                    break;
                default:                    // techno frame (like the background blocks)
                    if (iw > 0.5f && ih > 0.5f) Frame(iw - 0.24f, ih - 0.24f, 0.05f, d, Base + 4);
                    break;
            }
        }

        void BuildCircle(BlockStyle st, float r)
        {
            Color glow = st.Edge; glow.a = st.Glow;
            Quad(SpriteFactory.Glow(), Vector2.zero, Vector2.one * r * 4.2f, 0f, glow, Base);
            Quad(SpriteFactory.Fill(ShapeType.Circle), Vector2.zero, Vector2.one * (r + 0.07f) * 2f / 0.9f, 0f, new Color(0, 0, 0, 0.8f), Base + 1);
            Quad(SpriteFactory.Fill(ShapeType.Circle), Vector2.zero, Vector2.one * r * 2f / 0.9f, 0f, st.Edge, Base + 2);
            Quad(SpriteFactory.Fill(ShapeType.Circle), Vector2.zero, Vector2.one * (r - 0.09f) * 2f / 0.9f, 0f, st.Fill, Base + 3);
            Color d = st.Detail;
            switch (st.Pattern)
            {
                case BlockPattern.Shine:
                    Quad(SpriteFactory.Fill(ShapeType.Circle), new Vector2(-r * 0.38f, r * 0.38f), Vector2.one * r * 0.42f, 0f, d, Base + 4);
                    Quad(SpriteFactory.Fill(ShapeType.Circle), new Vector2(-r * 0.12f, r * 0.55f), Vector2.one * r * 0.18f, 0f, d, Base + 4);
                    break;
                case BlockPattern.Dots:
                case BlockPattern.Bricks:
                    for (int k = 0; k < 5; k++)
                    {
                        float a = k * 2.4f, rr = r * (0.25f + 0.12f * k);
                        Quad(SpriteFactory.Fill(ShapeType.Circle), new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * Mathf.Min(rr, r * 0.6f), Vector2.one * r * 0.28f, 0f, d, Base + 4);
                    }
                    break;
                case BlockPattern.Stripes:
                    Quad(SpriteFactory.Outline(ShapeType.Circle), Vector2.zero, Vector2.one * r * 1.3f, 0f, d, Base + 4);
                    Quad(SpriteFactory.Outline(ShapeType.Circle), Vector2.zero, Vector2.one * r * 0.7f, 0f, d, Base + 4);
                    break;
                default:
                    Quad(SpriteFactory.Outline(ShapeType.Circle), Vector2.zero, Vector2.one * r * 1.25f, 0f, d, Base + 4);
                    if (st.Pattern == BlockPattern.Rivets)
                        for (int k = 0; k < 4; k++)
                        {
                            float a = k * Mathf.PI / 2f + Mathf.PI / 4f;
                            Quad(SpriteFactory.Fill(ShapeType.Circle), new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r * 0.78f, Vector2.one * 0.12f, 0f, st.Edge, Base + 5);
                        }
                    break;
            }
        }

        void Frame(float w, float h, float t, Color c, int order)
        {
            Quad(SpriteFactory.Pixel(), new Vector2(0, h / 2), new Vector2(w, t), 0f, c, order);
            Quad(SpriteFactory.Pixel(), new Vector2(0, -h / 2), new Vector2(w, t), 0f, c, order);
            Quad(SpriteFactory.Pixel(), new Vector2(w / 2, 0), new Vector2(t, h), 0f, c, order);
            Quad(SpriteFactory.Pixel(), new Vector2(-w / 2, 0), new Vector2(t, h), 0f, c, order);
        }

        void Quad(Sprite s, Vector2 pos, Vector2 size, float rot, Color c, int order)
        {
            var g = new GameObject("Part");
            g.transform.SetParent(transform, false);
            g.transform.localPosition = pos;
            g.transform.localRotation = Quaternion.Euler(0, 0, rot);
            g.transform.localScale = new Vector3(size.x, size.y, 1f);
            var r = g.AddComponent<SpriteRenderer>();
            r.sprite = s; r.color = c; r.sortingOrder = order;
        }

        // ------------------------------------------------------------------ collision
        /// <summary>Moves a circle (position p, radius r) out of the obstacle if it overlaps.</summary>
        public Vector2 PushOut(Vector2 p, float r)
        {
            if (circle)
            {
                Vector2 d = p - center;
                float dist = d.magnitude, min = radius + r;
                if (dist >= min) return p;
                Vector2 n = dist > 0.0001f ? d / dist : Vector2.up;
                return center + n * min;
            }

            Vector2 q = new Vector2(Mathf.Clamp(p.x, center.x - half.x, center.x + half.x),
                                    Mathf.Clamp(p.y, center.y - half.y, center.y + half.y));
            Vector2 off = p - q;
            float od = off.magnitude;
            if (od >= r) return p;
            if (od > 0.0001f) return q + off / od * r;

            // centre is inside the rectangle: leave through the nearest side
            float dx = half.x - Mathf.Abs(p.x - center.x), dy = half.y - Mathf.Abs(p.y - center.y);
            if (dx < dy) return new Vector2(center.x + Mathf.Sign(p.x - center.x) * (half.x + r), p.y);
            return new Vector2(p.x, center.y + Mathf.Sign(p.y - center.y) * (half.y + r));
        }
    }
}
