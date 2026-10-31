using UnityEngine;

namespace TiltRush
{
    /// <summary>
    /// Permanent obstacle (campaign). It blocks the player AND the dots but never hurts.
    /// Hide behind it - dots slide around it.
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

            Color fill = new Color32(24, 30, 44, 255);
            Color line = new Color32(120, 140, 175, 255);
            if (def.Circle)
            {
                o.circle = true; o.radius = def.A;
                ShapeView.Create(go.transform, ShapeType.Circle, def.A, fill, line, 0.15f, -20);
            }
            else
            {
                o.half = new Vector2(def.A * 0.5f, def.B * 0.5f);
                o.Quad(go.transform, SpriteFactory.Glow(), new Vector2(def.A * 1.3f, def.B * 2.2f), new Color(line.r, line.g, line.b, 0.15f), -21);
                o.Quad(go.transform, SpriteFactory.Pixel(), new Vector2(def.A, def.B), line, -20);
                o.Quad(go.transform, SpriteFactory.Pixel(), new Vector2(def.A - 0.12f, def.B - 0.12f), fill, -19);
            }
            return o;
        }

        void Quad(Transform parent, Sprite s, Vector2 size, Color c, int order)
        {
            var g = new GameObject("Quad");
            g.transform.SetParent(parent, false);
            g.transform.localScale = new Vector3(size.x, size.y, 1f);
            var r = g.AddComponent<SpriteRenderer>();
            r.sprite = s; r.color = c; r.sortingOrder = order;
        }

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
