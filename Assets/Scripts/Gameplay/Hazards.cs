using UnityEngine;

namespace TiltRush
{
    /// <summary>A red bar that slowly sweeps across the arena. Dodge around its ends.</summary>
    public class Wall : MonoBehaviour
    {
        Vector2 dir;
        float length, thickness, traveled, travelDist, speed;
        public Vector2 Pos { get { return transform.position; } }

        public static Wall Spawn(Transform parent, Vector2 start, Vector2 dir, float length, float travelDist, float speed)
        {
            var go = new GameObject("Wall");
            go.transform.SetParent(parent, false);
            go.transform.position = start;
            var w = go.AddComponent<Wall>();
            w.dir = dir; w.length = length; w.thickness = Config.WallThickness;
            w.travelDist = travelDist; w.speed = speed;

            Vector2 u = new Vector2(-dir.y, dir.x);   // long axis is perpendicular to the travel direction
            go.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(u.y, u.x) * Mathf.Rad2Deg);

            // dark core + glowing red rim (readable on every background colour)
            w.Layer("Glow", SpriteFactory.Glow(), new Vector2(length * 1.15f, w.thickness * 5f), new Color(1f, 0.1f, 0.1f, 0.45f), 4);
            w.Layer("Border", SpriteFactory.Pixel(), new Vector2(length, w.thickness), new Color32(255, 70, 70, 255), 5);
            w.Layer("Inner", SpriteFactory.Pixel(), new Vector2(length - 0.12f, w.thickness - 0.12f), new Color32(60, 0, 12, 255), 6);
            return w;
        }

        void Layer(string n, Sprite s, Vector2 size, Color c, int order)
        {
            var g = new GameObject(n);
            g.transform.SetParent(transform, false);
            g.transform.localScale = new Vector3(size.x, size.y, 1f);
            var r = g.AddComponent<SpriteRenderer>();
            r.sprite = s; r.color = c; r.sortingOrder = order;
        }

        /// <returns>false when the wall has left the arena</returns>
        public bool Tick(float edt)
        {
            transform.position += (Vector3)(dir * speed * edt);
            traveled += speed * edt;
            return traveled < travelDist;
        }

        public bool Hits(Vector2 p, float r)
        {
            Vector2 u = new Vector2(-dir.y, dir.x);
            float t = Mathf.Clamp(Vector2.Dot(p - Pos, u), -length * 0.5f, length * 0.5f);
            Vector2 closest = Pos + u * t;
            float reach = thickness * 0.5f + r;
            return (p - closest).sqrMagnitude < reach * reach;
        }
    }

    /// <summary>
    /// A sword that races across the whole screen. A light red lane is shown first,
    /// then the sword flies along it.
    /// </summary>
    public class Sword : MonoBehaviour
    {
        public static readonly Color Marker = new Color32(255, 120, 120, 255);   // light red warning colour

        Vector2 start, dir;
        float warn, speed, traveled, travelDist, length;
        bool flying;
        GameObject warnRoot, blade;
        SpriteRenderer strip, edgeA, edgeB;

        public static Sword Spawn(Transform parent, Vector2 start, Vector2 dir, float travelDist, float warning, float speed)
        {
            var go = new GameObject("Sword");
            go.transform.SetParent(parent, false);
            var s = go.AddComponent<Sword>();
            s.start = start; s.dir = dir; s.travelDist = travelDist; s.warn = warning; s.speed = speed;
            s.length = Config.SwordLength;
            float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            // 1) warning lane
            s.warnRoot = new GameObject("Warning");
            s.warnRoot.transform.SetParent(go.transform, false);
            s.warnRoot.transform.position = start + dir * travelDist * 0.5f;
            s.warnRoot.transform.rotation = Quaternion.Euler(0, 0, ang);
            float lw = Config.SwordLaneWidth;
            s.strip = Quad(s.warnRoot.transform, new Vector2(travelDist, lw), Vector2.zero, -30);
            s.strip.color = new Color(0.1f, 0f, 0.02f, 1f);   // dark lane + light red edges: visible on every background
            s.edgeA = Quad(s.warnRoot.transform, new Vector2(travelDist, 0.06f), new Vector2(0, lw * 0.5f), -29);
            s.edgeB = Quad(s.warnRoot.transform, new Vector2(travelDist, 0.06f), new Vector2(0, -lw * 0.5f), -29);

            // 2) the blade itself (hidden until the lane has been shown)
            s.blade = new GameObject("Blade");
            s.blade.transform.SetParent(go.transform, false);
            Layer(s.blade.transform, SpriteFactory.Glow(), Config.SwordLength * 1.4f, new Color(1f, 0.2f, 0.2f, 0.45f), 8);
            Layer(s.blade.transform, SpriteFactory.BladeFill(), Config.SwordLength, new Color32(235, 235, 255, 255), 9);
            Layer(s.blade.transform, SpriteFactory.BladeLine(), Config.SwordLength, new Color32(255, 60, 60, 255), 10);
            s.blade.SetActive(false);
            return s;
        }

        static SpriteRenderer Quad(Transform parent, Vector2 size, Vector2 localPos, int order)
        {
            var g = new GameObject("Quad");
            g.transform.SetParent(parent, false);
            g.transform.localPosition = localPos;
            g.transform.localScale = new Vector3(size.x, size.y, 1f);
            var r = g.AddComponent<SpriteRenderer>();
            r.sprite = SpriteFactory.Pixel(); r.color = Marker; r.sortingOrder = order;
            return r;
        }

        static void Layer(Transform parent, Sprite sprite, float scale, Color c, int order)
        {
            var g = new GameObject("Layer");
            g.transform.SetParent(parent, false);
            g.transform.localScale = Vector3.one * scale;
            var r = g.AddComponent<SpriteRenderer>();
            r.sprite = sprite; r.color = c; r.sortingOrder = order;
        }

        static void SetAlpha(SpriteRenderer r, float a) { var c = r.color; c.a = Mathf.Clamp01(a); r.color = c; }

        /// <returns>false when the sword has left the screen</returns>
        public bool Tick(float dt, float edt)
        {
            if (!flying)
            {
                warn -= dt;
                float a = 0.16f + 0.14f * Mathf.Sin(Time.time * (warn < 0.5f ? 30f : 16f));   // blinks faster right before the sword comes
                SetAlpha(strip, a * 2f);
                SetAlpha(edgeA, a * 3f);
                SetAlpha(edgeB, a * 3f);
                if (warn <= 0f) { flying = true; warnRoot.SetActive(false); blade.SetActive(true); Place(); }
                return true;
            }
            traveled += speed * edt;
            Place();
            return traveled < travelDist;
        }

        void Place()
        {
            blade.transform.position = start + dir * traveled;
            blade.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f);   // sprite tip points up
        }

        public bool Hits(Vector2 p, float r)
        {
            if (!flying) return false;
            Vector2 c = start + dir * traveled;
            Vector2 a = c - dir * length * 0.45f, b = c + dir * length * 0.49f;
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            float reach = 0.2f + r;
            return (p - (a + ab * t)).sqrMagnitude < reach * reach;
        }
    }
}
