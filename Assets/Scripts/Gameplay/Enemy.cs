using UnityEngine;

namespace TiltRush
{
    public enum EnemyType { Normal, Fast, Big }

    /// <summary>A red dot that hunts the nearest player. Edit the table below to tune / add enemy types.</summary>
    public class Enemy : MonoBehaviour
    {
        struct Def
        {
            public float Speed, Radius, Spin; public ShapeType Shape;
            public Def(float s, float r, ShapeType shape, float spin) { Speed = s; Radius = r; Shape = shape; Spin = spin; }
        }

        // Speed, hit radius, look, spin (degrees per second). Colours come from GameTheme (they change with difficulty).
        static readonly Def[] defs =
        {
            new Def(2.0f, 0.13f, ShapeType.Saw, 200f),       // Normal: saw blade with a glowing eye
            new Def(2.8f, 0.095f, ShapeType.Spike6, 330f),   // Fast: small spiky star
            new Def(1.35f, 0.24f, ShapeType.Spike10, 50f),   // Big: spiky ball with angry eyes
        };

        // The picture is bigger than the hit radius: the body matches the hit circle, the spikes stick out (forgiving).
        const float VisualScale = 1.3f;

        public EnemyType Type;
        public bool Dead;
        public float Radius;
        public bool Dangerous { get { return warm <= 0f; } }
        public Color SparkColor { get { return GameTheme.EnemyRim(Type); } }
        public Vector2 Pos { get { return transform.position; } }

        ShapeView view;
        Transform body;                 // pop scale (the view inside spins)
        SpriteRenderer eye;             // Normal/Fast: one glowing eye; Big: two angry eyes
        Vector2 look;
        float spinDir = 1f, spinAngle;
        float speed, warm, phase, age, bump;
        const float PopTime = 0.28f, BumpTime = 0.2f;

        /// <summary>0 -> 1 with a little overshoot ("pop").</summary>
        public static float EaseOutBack(float x)
        {
            x = Mathf.Clamp01(x) - 1f;
            return 1f + 2.70158f * x * x * x + 1.70158f * x * x;
        }

        /// <summary>Creates a dot. GameWorld keeps killed dots in a pool and reuses them via Setup.</summary>
        public static Enemy Create(Transform parent)
        {
            var go = new GameObject("Enemy");
            go.transform.SetParent(parent, false);
            var e = go.AddComponent<Enemy>();
            e.body = new GameObject("Body").transform;
            e.body.SetParent(go.transform, false);
            e.view = ShapeView.Create(e.body, ShapeType.Saw, defs[0].Radius, Color.black, Color.white, 0.4f, 5);

            var eg = new GameObject("Eye");
            eg.transform.SetParent(e.body, false);
            e.eye = eg.AddComponent<SpriteRenderer>();
            e.eye.sortingOrder = 7;
            return e;
        }

        public void Setup(EnemyType type, Vector2 pos, float speedMul, float phase)
        {
            var d = defs[(int)type];
            gameObject.SetActive(true);
            transform.position = pos;
            Type = type; Dead = false; Radius = d.Radius; speed = d.Speed * speedMul;
            warm = Config.EnemySpawnWarning; this.phase = phase;
            age = 0f; bump = 0f; look = Vector2.zero;
            spinDir = phase > 3.14f ? 1f : -1f; spinAngle = phase * 57.3f;
            body.localScale = Vector3.zero;

            view.SetShape(d.Shape);
            view.SetRadius(d.Radius * VisualScale);
            bool big = type == EnemyType.Big;
            eye.sprite = big ? SpriteFactory.AngryEyes() : SpriteFactory.Fill(ShapeType.Circle);
            eye.transform.localScale = Vector3.one * (big ? d.Radius * 1.5f : d.Radius * 0.62f);
            ApplyColors(0.3f);
        }

        void ApplyColors(float alpha)
        {
            view.SetColors(GameTheme.EnemyBody, GameTheme.EnemyRim(Type));
            view.SetGlow(GameTheme.EnemyGlow);
            view.Alpha = alpha;
            Color e = GameTheme.EnemyEye; e.a = alpha; eye.color = e;
        }

        /// <param name="edt">enemy delta time (slow-mo / freeze applied)</param>
        public void Tick(float edt, float dt, GameWorld w)
        {
            // pop in when spawning, little bump when the dot becomes dangerous
            age += dt;
            float s = age < PopTime ? EaseOutBack(age / PopTime) : 1f;
            if (bump > 0f) { bump -= dt; s *= 1f + 0.45f * Mathf.Sin(Mathf.PI * Mathf.Clamp01(1f - bump / BumpTime)); }
            body.localScale = Vector3.one * s;
            spinAngle += spinDir * defs[(int)Type].Spin * edt;                 // frozen / slow-mo dots spin slower too
            view.transform.localRotation = Quaternion.Euler(0, 0, spinAngle);
            eye.transform.localPosition = look * Radius * 0.35f;      // the eyes look where the dot is going

            if (warm > 0f)
            {
                warm -= dt;
                ApplyColors(warm > 0f ? 0.25f + 0.2f * Mathf.Sin(Time.time * 20f) : 1f);
                if (warm <= 0f) bump = BumpTime;
                return;
            }
            ApplyColors(1f);   // colours follow the difficulty

            Vector2 targetPos;
            if (!w.NearestTarget(Pos, out targetPos)) return;   // nearest player or decoy

            Vector2 dir = targetPos - Pos;
            float d = dir.magnitude;
            if (d > 0.001f) dir /= d;
            Vector2 perp = new Vector2(-dir.y, dir.x);
            look = Vector2.Lerp(look, dir, 1f - Mathf.Exp(-6f * dt));
            Vector2 move = (dir + perp * Mathf.Sin(w.Elapsed * 2f + phase) * 0.25f).normalized * speed * edt;  // slight weave so swarms spread out
            transform.position = w.PushOut(Pos + move, Radius);   // obstacles block dots too
        }
    }
}
