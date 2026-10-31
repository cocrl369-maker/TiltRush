using UnityEngine;

namespace TiltRush
{
    /// <summary>Short-lived effects: expanding rings, beams, lightning, tracers.</summary>
    public class Fx : MonoBehaviour
    {
        static int alive;
        ShapeView view;
        float t, dur, r0, r1;

        public static void Burst(Vector2 pos, Color c, float radiusStart, float radiusEnd, float duration)
        {
            if (alive > 70) return;   // safety cap for nukes
            var go = new GameObject("Fx");
            go.transform.position = pos;
            var f = go.AddComponent<Fx>();
            f.view = ShapeView.Create(go.transform, ShapeType.Circle, radiusStart, Color.clear, c, 0.3f, 20);
            f.r0 = radiusStart; f.r1 = radiusEnd; f.dur = duration;
            alive++;
        }

        /// <summary>Wide soft beam with a bright core (Lance gadget).</summary>
        public static void Beam(Vector2 from, Vector2 dir, float length, Color c)
        {
            Vector2 to = from + dir * length;
            FxLine.Create(from, to, 0.7f, new Color(c.r, c.g, c.b, 0.35f), 0.4f, 18);
            FxLine.Create(from, to, 0.22f, Color.white, 0.3f, 19);
            Burst(from, c, 0.2f, 1.2f, 0.3f);
        }

        /// <summary>Jagged lightning bolt between two points.</summary>
        public static void Bolt(Vector2 a, Vector2 b, Color c)
        {
            const int segments = 6;
            Vector2 d = b - a;
            Vector2 n = new Vector2(-d.y, d.x).normalized;
            Vector2 prev = a;
            for (int i = 1; i <= segments; i++)
            {
                Vector2 p = i == segments ? b : a + d * (i / (float)segments) + n * Random.Range(-0.35f, 0.35f);
                FxLine.Create(prev, p, 0.35f, new Color(c.r, c.g, c.b, 0.3f), 0.18f, 18);
                FxLine.Create(prev, p, 0.1f, Color.white, 0.18f, 19);
                prev = p;
            }
        }

        /// <summary>Thin straight shot (turret).</summary>
        public static void Tracer(Vector2 a, Vector2 b, Color c) { FxLine.Create(a, b, 0.09f, c, 0.12f, 19); }

        void Update()
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / dur);
            view.SetRadius(Mathf.Lerp(r0, r1, k));
            view.Alpha = 1f - k;
            if (k >= 1f) Destroy(gameObject);
        }

        void OnDestroy() { alive--; }
    }

    /// <summary>A straight line segment that fades out.</summary>
    public class FxLine : MonoBehaviour
    {
        static int alive;
        SpriteRenderer sr;
        float t, dur, len, width;
        Color color;

        public static void Create(Vector2 a, Vector2 b, float width, Color color, float duration, int order)
        {
            if (alive > 160) return;
            Vector2 d = b - a;
            float len = d.magnitude;
            if (len < 0.01f) return;

            var go = new GameObject("FxLine");
            go.transform.position = (a + b) * 0.5f;
            go.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            var f = go.AddComponent<FxLine>();
            f.sr = go.AddComponent<SpriteRenderer>();
            f.sr.sprite = SpriteFactory.Pixel();
            f.sr.sortingOrder = order;
            f.len = len; f.width = width; f.color = color; f.dur = duration;
            alive++;
            f.Apply(0f);
        }

        void Update()
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / dur);
            Apply(k);
            if (k >= 1f) Destroy(gameObject);
        }

        void Apply(float k)
        {
            transform.localScale = new Vector3(len, width * (1f - k * 0.7f), 1f);
            sr.color = new Color(color.r, color.g, color.b, color.a * (1f - k));
        }

        void OnDestroy() { alive--; }
    }
}
