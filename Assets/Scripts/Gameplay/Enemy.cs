using UnityEngine;

namespace TiltRush
{
    public enum EnemyType { Normal, Fast, Big, Asteroid, Rock, Pebble, Shard }

    /// <summary>A red dot that hunts the nearest player. Edit the table below to tune / add enemy types.</summary>
    public class Enemy : MonoBehaviour
    {
        struct Def
        {
            public float Speed, Radius, Spin, Visual; public ShapeType Shape; public int Hp;
            public Def(float s, float r, ShapeType shape, float spin, float visual = 1.3f, int hp = 1)
            { Speed = s; Radius = r; Shape = shape; Spin = spin; Visual = visual; Hp = hp; }
        }

        // Speed, hit radius, look, spin (degrees per second). Colours come from GameTheme (they change with difficulty).
        static readonly Def[] defs =
        {
            new Def(2.0f, 0.13f, ShapeType.Saw, 200f),       // Normal: saw blade with a glowing eye
            new Def(2.8f, 0.095f, ShapeType.Spike6, 330f),   // Fast: small spiky star
            new Def(1.35f, 0.24f, ShapeType.Spike10, 50f),   // Big: spiky ball with angry eyes
            // asteroid field (they drift in a straight line; speed = drift speed, Hp = shots needed)
            new Def(1.4f, 0.55f, ShapeType.Rock, 25f, 1.1f, 6),     // Asteroid: splits into 2 rocks
            new Def(2.0f, 0.34f, ShapeType.Rock2, 50f, 1.1f, 3),    // Rock: splits into 2 pebbles
            new Def(2.8f, 0.2f, ShapeType.Rock, 90f, 1.1f, 1),      // Pebble
            new Def(5.5f, 0.12f, ShapeType.Shard, 320f, 1.2f, 1),   // Shard: fast debris
        };

        public static bool IsRock(EnemyType t) { return t >= EnemyType.Asteroid; }


        public EnemyType Type;
        public bool Dead;
        public float Radius;
        public bool Dangerous { get { return warm <= 0f && FrozenTime <= 0f; } }
        public bool Frozen { get { return FrozenTime > 0f; } }
        public float FrozenTime;

        public void Freeze(float seconds) { FrozenTime = Mathf.Max(FrozenTime, seconds); }

        // asteroid field: straight drift, hit points, flash when hit
        public Vector2 Drift;
        public bool Drifting, Gone;     // Gone = left the arena (removed without points)
        public int Hp;
        float flash;

        /// <summary>Turns this enemy into a drifting asteroid / shard (no chasing).</summary>
        public void MakeDrifter(Vector2 velocity)
        {
            Drifting = true; Drift = velocity; warm = 0f; Hp = defs[(int)Type].Hp;
        }

        /// <returns>true when destroyed</returns>
        public bool Hit(int damage)
        {
            Hp -= damage; flash = 0.08f;
            return Hp <= 0;
        }
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
            age = 0f; bump = 0f; look = Vector2.zero; FrozenTime = 0f;
            Drifting = false; Gone = false; Drift = Vector2.zero; Hp = d.Hp; flash = 0f;
            spinDir = phase > 3.14f ? 1f : -1f; spinAngle = phase * 57.3f;
            body.localScale = Vector3.zero;

            view.SetShape(d.Shape);
            view.SetRadius(d.Radius * d.Visual);   // spikes stick out of the hit circle (forgiving)
            bool big = type == EnemyType.Big, rock = IsRock(type);
            eye.sprite = rock ? SpriteFactory.Craters() : (big ? SpriteFactory.AngryEyes() : SpriteFactory.Fill(ShapeType.Circle));
            eye.enabled = type != EnemyType.Shard;
            eye.transform.localScale = Vector3.one * (rock ? d.Radius * 2f : (big ? d.Radius * 1.5f : d.Radius * 0.62f));
            ApplyColors(0.3f);
        }

        void ApplyColors(float alpha)
        {
            Color rim = GameTheme.EnemyRim(Type);
            Color bodyC = IsRock(Type) ? new Color32(58, 44, 40, 255) : GameTheme.EnemyBody;
            if (flash > 0f) { bodyC = Color.Lerp(bodyC, Color.white, 0.8f); rim = Color.white; }   // hit flash
            view.SetColors(bodyC, rim);
            view.SetGlow(GameTheme.EnemyGlow);
            view.Alpha = alpha;
            Color e = IsRock(Type) ? new Color(rim.r, rim.g, rim.b, 0.45f * alpha) : GameTheme.EnemyEye;
            if (!IsRock(Type)) e.a = alpha;
            eye.color = e;
        }

        /// <param name="edt">enemy delta time (slow-mo / freeze applied)</param>
        public void Tick(float edt, float dt, GameWorld w)
        {
            // pop in when spawning, little bump when the dot becomes dangerous
            age += dt;
            float s = age < PopTime ? EaseOutBack(age / PopTime) : 1f;
            if (bump > 0f) { bump -= dt; s *= 1f + 0.45f * Mathf.Sin(Mathf.PI * Mathf.Clamp01(1f - bump / BumpTime)); }
            body.localScale = Vector3.one * s;

            if (FrozenTime > 0f)                       // ice block: no movement, icy colours, blinks before it thaws
            {
                FrozenTime -= dt;
                float a = FrozenTime < 1f && Mathf.Sin(Time.time * 22f) > 0f ? 0.55f : 1f;
                view.SetColors(new Color32(130, 200, 250, 255), Color.white);
                view.SetGlow(0.55f);
                view.Alpha = a;
                eye.color = new Color(1f, 1f, 1f, 0.6f * a);
                return;
            }
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

            if (Drifting)                              // asteroid field: fly straight, leave the arena on the other side
            {
                if (flash > 0f) flash -= dt;
                transform.position = Pos + Drift * edt;
                Vector2 half = Config.ArenaSize * 0.5f + Vector2.one * (Radius + 1.5f);
                if ((Mathf.Abs(Pos.x) > half.x && Pos.x * Drift.x > 0f) || (Mathf.Abs(Pos.y) > half.y && Pos.y * Drift.y > 0f)) Gone = true;
                return;
            }

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
