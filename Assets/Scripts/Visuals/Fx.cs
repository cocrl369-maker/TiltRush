using System.Collections.Generic;
using UnityEngine;

namespace TiltRush
{
    /// <summary>Short-lived effects: expanding rings, beams, lightning, tracers.</summary>
    public class Fx : MonoBehaviour
    {
        static int alive;
        static readonly Stack<Fx> pool = new Stack<Fx>();   // finished effects are reused, not destroyed
        ShapeView view;
        float t, dur, r0, r1;

        public static void Burst(Vector2 pos, Color c, float radiusStart, float radiusEnd, float duration)
        {
            if (alive > 70) return;   // safety cap for nukes
            Fx f = null;
            while (f == null && pool.Count > 0) f = pool.Pop();   // (skips entries Unity has destroyed)
            if (f == null)
            {
                var go = new GameObject("Fx");
                f = go.AddComponent<Fx>();
                f.view = ShapeView.Create(go.transform, ShapeType.Circle, radiusStart, Color.clear, c, 0.3f, 20);
            }
            else f.gameObject.SetActive(true);
            f.transform.position = pos;
            f.view.SetColors(Color.clear, c);
            f.view.SetRadius(radiusStart);
            f.view.Alpha = 1f;
            f.t = 0f; f.r0 = radiusStart; f.r1 = radiusEnd; f.dur = Mathf.Max(0.01f, duration);
            alive++;
        }

        /// <summary>Glowing sparks flying out of a point (see SparkSystem).</summary>
        public static void Sparks(Vector2 pos, Color c, int count, float speed = 6f, float life = 0.45f, float width = 0.06f)
        {
            int setting = SaveManager.Data.particles;           // Options > Graphics > Particles
            if (setting <= 0) return;
            if (setting == 1) count = (count + 1) / 2;
            SparkSystem.Emit(pos, c, count, speed, life, width);
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
            if (k >= 1f) { gameObject.SetActive(false); alive--; pool.Push(this); }
        }

        void OnDestroy() { if (gameObject.activeSelf) alive--; }
    }

    /// <summary>A straight line segment that fades out.</summary>
    public class FxLine : MonoBehaviour
    {
        static int alive;
        static readonly Stack<FxLine> pool = new Stack<FxLine>();
        SpriteRenderer sr;
        float t, dur, len, width;
        Color color;

        public static void Create(Vector2 a, Vector2 b, float width, Color color, float duration, int order)
        {
            if (alive > 160) return;
            Vector2 d = b - a;
            float len = d.magnitude;
            if (len < 0.01f) return;

            FxLine f = null;
            while (f == null && pool.Count > 0) f = pool.Pop();
            if (f == null)
            {
                var go = new GameObject("FxLine");
                f = go.AddComponent<FxLine>();
                f.sr = go.AddComponent<SpriteRenderer>();
                f.sr.sprite = SpriteFactory.Pixel();
            }
            else f.gameObject.SetActive(true);
            f.transform.position = (a + b) * 0.5f;
            f.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            f.sr.sortingOrder = order;
            f.t = 0f; f.len = len; f.width = width; f.color = color; f.dur = Mathf.Max(0.01f, duration);
            alive++;
            f.Apply(0f);
        }

        void Update()
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / dur);
            Apply(k);
            if (k >= 1f) { gameObject.SetActive(false); alive--; pool.Push(this); }
        }

        void Apply(float k)
        {
            transform.localScale = new Vector3(len, width * (1f - k * 0.7f), 1f);
            sr.color = new Color(color.r, color.g, color.b, color.a * (1f - k));
        }

        void OnDestroy() { if (gameObject.activeSelf) alive--; }
    }
}

namespace TiltRush
{
    /// <summary>
    /// Sparks: small glowing streaks that fly out and fade (dot destroyed, player hit, pickups, launch rings).
    /// One component draws all sparks with a fixed set of renderers (ring buffer: the oldest spark is reused),
    /// so even a nuke with hundreds of kills costs nothing extra. Purely visual - uses UnityEngine.Random,
    /// never the seeded game RNGs.
    /// </summary>
    public class SparkSystem : MonoBehaviour
    {
        const int Max = 320;
        static SparkSystem inst;

        SpriteRenderer[] rs;
        Vector2[] pos, vel;
        float[] life, maxLife, size;
        Color[] col;
        int next;

        static SparkSystem I
        {
            get
            {
                if (inst == null) inst = new GameObject("Sparks").AddComponent<SparkSystem>();
                return inst;
            }
        }

        void Awake()
        {
            rs = new SpriteRenderer[Max];
            pos = new Vector2[Max]; vel = new Vector2[Max];
            life = new float[Max]; maxLife = new float[Max]; size = new float[Max];
            col = new Color[Max];
            for (int i = 0; i < Max; i++)
            {
                var g = new GameObject("Spark");
                g.transform.SetParent(transform, false);
                rs[i] = g.AddComponent<SpriteRenderer>();
                rs[i].sprite = SpriteFactory.Pixel();
                rs[i].sortingOrder = 19;
                rs[i].enabled = false;
            }
        }

        public static void Emit(Vector2 at, Color c, int count, float speed, float lifetime, float width)
        {
            var s = I;
            for (int n = 0; n < count; n++)
            {
                int i = s.next; s.next = (s.next + 1) % Max;
                float a = Random.value * 6.2832f;
                float sp = speed * Random.Range(0.45f, 1.15f);
                s.pos[i] = at;
                s.vel[i] = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * sp;
                s.maxLife[i] = s.life[i] = lifetime * Random.Range(0.6f, 1.2f);
                s.size[i] = width * Random.Range(0.7f, 1.2f);
                // a few sparks are white-hot, the rest have the colour of the source
                s.col[i] = Random.value < 0.25f ? Color.Lerp(c, Color.white, 0.7f) : c;
                s.rs[i].enabled = true;
            }
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;                                   // paused
            float drag = Mathf.Exp(-4f * dt);
            for (int i = 0; i < Max; i++)
            {
                if (!rs[i].enabled) continue;
                life[i] -= dt;
                if (life[i] <= 0f) { rs[i].enabled = false; continue; }
                vel[i] *= drag;
                pos[i] += vel[i] * dt;
                float k = life[i] / maxLife[i];                      // 1 -> 0
                float speed = vel[i].magnitude;
                var t = rs[i].transform;
                t.position = pos[i];
                if (speed > 0.01f) t.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(vel[i].y, vel[i].x) * Mathf.Rad2Deg);
                t.localScale = new Vector3(Mathf.Max(size[i], speed * 0.035f), size[i] * (0.4f + 0.6f * k), 1f);   // streak
                Color c = col[i]; c.a *= k; rs[i].color = c;
            }
        }
    }
}
