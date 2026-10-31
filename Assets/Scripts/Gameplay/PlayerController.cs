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

        public void Init(int index, IMoveInput controls, Vector2 pos, PlayerStyle style)
        {
            Index = index; Controls = controls;
            transform.position = pos;
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
        }
    }
}
