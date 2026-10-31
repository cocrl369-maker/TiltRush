using UnityEngine;

namespace TiltRush
{
    public class PlayerController : MonoBehaviour
    {
        public int Index;
        public bool Alive = true;
        public float ShieldTime;
        public float StormTime, StormCd;        // lightning gadget
        public float Radius = Config.PlayerRadius;
        public Vector2 Facing = Vector2.up;     // direction the tip points to
        public IMoveInput Controls;

        public Vector2 Pos { get { return transform.position; } }
        public Vector2 Velocity { get { return vel + launchVel; } }

        ShapeView view, shield, storm;
        Vector2 vel, launchVel;
        float angle;
        PlayerStyle style;

        // light trail: a tapered tail of soft dots at the last positions
        const int TrailLen = 16;
        const float TrailSample = 1f / 60f;
        SpriteRenderer[] trail;
        readonly Vector2[] trailPos = new Vector2[TrailLen];
        int trailHead;
        float trailTimer;

        public void Init(int index, IMoveInput controls, Vector2 pos, PlayerStyle style)
        {
            Index = index; Controls = controls; this.style = style;
            transform.position = pos;
            BuildTrail(pos);
            view = ShapeView.Create(transform, style.Shape, Radius, style.Primary, style.Outline, 0.5f, 10);
            shield = ShapeView.Create(transform, ShapeType.Circle, 0.5f, new Color(0f, 1f, 0.4f, 0.12f), Neon.Green, 0.5f, 12);
            shield.gameObject.SetActive(false);
            storm = ShapeView.Create(transform, ShapeType.Circle, 0.42f, Color.clear, new Color32(150, 130, 255, 255), 0.6f, 13);
            storm.gameObject.SetActive(false);
        }

        public void Tick(float dt)
        {
            if (!Alive) return;

            Vector2 wanted = Controls.Read() * Config.PlayerMaxSpeed;
            vel = Vector2.MoveTowards(vel, wanted, Config.PlayerAcceleration * dt);
            launchVel *= Mathf.Exp(-Config.LaunchDamping * dt);

            Vector2 p = Pos + (vel + launchVel) * dt;

            // permanent obstacles block the way (they never hurt)
            var world = GameWorld.Current;
            if (world != null)
            {
                Vector2 pushed = world.PushOut(p, Radius);
                if (pushed != p)
                {
                    Vector2 n = (pushed - p).normalized;
                    float vn = Vector2.Dot(vel, n);
                    if (vn < 0f) vel -= n * vn;
                    float ln = Vector2.Dot(launchVel, n);
                    if (ln < 0f) launchVel -= n * ln;
                    p = pushed;
                }
            }

            Vector2 half = Config.ArenaSize * 0.5f - Vector2.one * Radius;
            if (Mathf.Abs(p.x) > half.x) { p.x = Mathf.Sign(p.x) * half.x; vel.x = 0; launchVel.x *= -0.4f; }
            if (Mathf.Abs(p.y) > half.y) { p.y = Mathf.Sign(p.y) * half.y; vel.y = 0; launchVel.y *= -0.4f; }
            transform.position = p;

            // the sharp tip always points where you are going
            if (vel.sqrMagnitude > 0.25f)
            {
                Facing = vel.normalized;
                float targetAngle = Mathf.Atan2(vel.y, vel.x) * Mathf.Rad2Deg - 90f;
                angle = Mathf.LerpAngle(angle, targetAngle, 1f - Mathf.Exp(-18f * dt));
            }
            view.transform.localRotation = Quaternion.Euler(0, 0, angle);

            if (ShieldTime > 0f)
            {
                ShieldTime -= dt;
                shield.gameObject.SetActive(ShieldTime > 0f);
                shield.Alpha = ShieldTime < 1.5f ? (Mathf.Sin(Time.time * 25f) > 0 ? 1f : 0.3f) : 1f;
            }

            if (StormTime > 0f)
            {
                StormTime -= dt;
                storm.gameObject.SetActive(StormTime > 0f);
                storm.Alpha = 0.5f + 0.5f * Mathf.Abs(Mathf.Sin(Time.time * 35f));
            }
        }

        void BuildTrail(Vector2 pos)
        {
            var root = new GameObject("Trail").transform;
            root.SetParent(transform.parent, false);           // world space, not attached to the moving ship
            trail = new SpriteRenderer[TrailLen];
            for (int i = 0; i < TrailLen; i++)
            {
                var g = new GameObject("T");
                g.transform.SetParent(root, false);
                var r = g.AddComponent<SpriteRenderer>();
                r.sprite = SpriteFactory.Fill(ShapeType.Circle);
                r.sortingOrder = 8;                             // below the ship (10)
                trail[i] = r;
                trailPos[i] = pos;
            }
        }

        void LateUpdate()
        {
            if (trail == null) return;
            if (!Alive) { foreach (var r in trail) r.enabled = false; return; }

            trailTimer += Time.deltaTime;                       // (stops while paused)
            while (trailTimer >= TrailSample)
            {
                trailTimer -= TrailSample;
                trailHead = (trailHead + 1) % TrailLen;
                trailPos[trailHead] = Pos;
            }

            Color c = Color.Lerp(style.Primary, style.Outline, 0.25f);
            float speed01 = Mathf.Clamp01(Velocity.magnitude / Config.PlayerMaxSpeed);
            for (int i = 0; i < TrailLen; i++)
            {
                var r = trail[i];
                Vector2 p = trailPos[(trailHead - i + TrailLen) % TrailLen];
                float k = 1f - i / (float)TrailLen;                                  // 1 at the ship, 0 at the end
                r.transform.position = p;
                r.transform.localScale = Vector3.one * (Radius * 1.7f * k + 0.02f);
                r.color = new Color(c.r, c.g, c.b, 0.45f * k * k * speed01);
                r.enabled = speed01 > 0.02f;
            }
        }

        public void GiveShield(float seconds) { ShieldTime = seconds; shield.gameObject.SetActive(true); }
        public void GiveStorm(float seconds) { StormTime = seconds; StormCd = 0f; storm.gameObject.SetActive(true); }
        public void Launch(Vector2 impulse) { launchVel = impulse; }

        public void Kill()
        {
            Alive = false;
            view.gameObject.SetActive(false);
            shield.gameObject.SetActive(false);
            storm.gameObject.SetActive(false);
            Fx.Burst(Pos, Color.white, 0.2f, 2.5f, 0.6f);
            Fx.Sparks(Pos, style.Primary, 26, 9f, 0.7f, 0.08f);
            Fx.Sparks(Pos, Color.white, 14, 6f, 0.5f, 0.06f);
        }
    }
}
