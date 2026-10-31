using UnityEngine;

namespace TiltRush
{
    /// <summary>Homing dart: hunts the nearest dot, destroys it and vanishes.</summary>
    public class Seeker : MonoBehaviour
    {
        Vector2 vel;
        float life = 5f;
        Enemy target;
        ShapeView view;
        public Vector2 Pos { get { return transform.position; } }

        public static Seeker Spawn(Transform parent, Vector2 pos, Vector2 dir)
        {
            var go = new GameObject("Seeker");
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var s = go.AddComponent<Seeker>();
            s.vel = dir * 6f;
            s.view = ShapeView.Create(go.transform, ShapeType.Arrow, 0.14f, new Color(0.5f, 1f, 0f, 0.35f), Neon.Lime, 0.7f, 9);
            return s;
        }

        /// <returns>false when it is used up</returns>
        public bool Tick(float dt, GameWorld w)
        {
            life -= dt;
            if (life <= 0f) { Fx.Burst(Pos, Neon.Lime, 0.1f, 0.5f, 0.25f); return false; }

            if (target == null || target.Dead) target = w.NearestEnemy(Pos, float.MaxValue);
            Vector2 desired = target != null ? (target.Pos - Pos).normalized * 10f : vel;
            vel = Vector2.MoveTowards(vel, desired, 45f * dt);
            transform.position = Pos + vel * dt;
            if (vel.sqrMagnitude > 0.01f)
                view.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(vel.y, vel.x) * Mathf.Rad2Deg - 90f);

            if (target != null)
            {
                float reach = target.Radius + 0.14f;
                if ((target.Pos - Pos).sqrMagnitude < reach * reach) { w.KillEnemyObj(target); return false; }
            }
            return true;
        }
    }

    /// <summary>Stationary turret: shoots the nearest dot every third of a second for a few seconds.</summary>
    public class Turret : MonoBehaviour
    {
        static readonly Color Shot = new Color32(230, 120, 255, 255);
        float life = 8f, cooldown = 0.3f;
        ShapeView body;
        Transform barrel;
        public Vector2 Pos { get { return transform.position; } }

        public static Turret Spawn(Transform parent, Vector2 pos)
        {
            var go = new GameObject("Turret");
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var t = go.AddComponent<Turret>();
            t.body = ShapeView.Create(go.transform, ShapeType.Diamond, 0.3f, new Color(0.8f, 0f, 1f, 0.3f), Neon.Purple, 0.6f, 8);

            var b = new GameObject("Barrel");
            b.transform.SetParent(go.transform, false);
            t.barrel = b.transform;
            var q = new GameObject("Tube");
            q.transform.SetParent(b.transform, false);
            q.transform.localPosition = new Vector3(0.25f, 0f, 0f);
            q.transform.localScale = new Vector3(0.5f, 0.1f, 1f);
            var r = q.AddComponent<SpriteRenderer>();
            r.sprite = SpriteFactory.Pixel(); r.color = Neon.Purple; r.sortingOrder = 9;
            return t;
        }

        public bool Tick(float dt, GameWorld w)
        {
            life -= dt; cooldown -= dt;
            var e = w.NearestEnemy(Pos, 9f);
            if (e != null)
            {
                Vector2 d = e.Pos - Pos;
                barrel.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
                if (cooldown <= 0f)
                {
                    cooldown = 0.32f;
                    Fx.Tracer(Pos, e.Pos, Shot);
                    w.KillEnemyObj(e);
                }
            }
            body.transform.localScale = Vector3.one * (1f + 0.06f * Mathf.Sin(Time.time * 10f));
            body.Alpha = life < 1.5f ? (Mathf.Sin(Time.time * 20f) > 0 ? 1f : 0.3f) : 1f;
            if (life <= 0f) { Fx.Burst(Pos, Neon.Purple, 0.2f, 1f, 0.3f); return false; }
            return true;
        }
    }

    /// <summary>Hologram copy of your ship: the dots chase it instead of you. Dots that touch it are destroyed.</summary>
    public class Decoy : MonoBehaviour
    {
        public const float Radius = 0.3f;
        static readonly Color Mint = new Color32(120, 255, 200, 255);
        float life = 7f;
        int hp = 14;
        ShapeView view, ring;
        public Vector2 Pos { get { return transform.position; } }

        public static Decoy Spawn(Transform parent, Vector2 pos, PlayerStyle style)
        {
            var go = new GameObject("Decoy");
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var d = go.AddComponent<Decoy>();
            d.view = ShapeView.Create(go.transform, style.Shape, Config.PlayerRadius, style.Primary, style.Outline, 0.5f, 9);
            d.view.SetDetail(style.Detail, style.Outline);
            d.ring = ShapeView.Create(go.transform, ShapeType.Circle, 0.5f, Color.clear, Mint, 0.4f, 8);
            d.view.Alpha = 0.55f; d.ring.Alpha = 0.6f;
            return d;
        }

        public void Hit()
        {
            hp--;
            Fx.Burst(Pos, Mint, 0.2f, 0.7f, 0.25f);
        }

        public bool Tick(float dt)
        {
            life -= dt;
            view.transform.Rotate(0, 0, 90f * dt);
            ring.transform.localScale = Vector3.one * (1f + 0.1f * Mathf.Sin(Time.time * 6f));
            float a = life < 1.5f ? (Mathf.Sin(Time.time * 20f) > 0 ? 0.6f : 0.2f) : 0.6f;
            view.Alpha = a; ring.Alpha = a;
            if (life <= 0f || hp <= 0) { Fx.Burst(Pos, Mint, 0.3f, 1.4f, 0.4f); return false; }
            return true;
        }
    }
}
