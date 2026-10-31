using UnityEngine;

namespace TiltRush
{
    /// <summary>
    /// Yellow ring like the GD yellow orb. Touching it catapults you BACKWARDS (opposite to your current
    /// movement). Rings relocate on a fixed timer (not when used) so online rounds stay identical.
    /// </summary>
    public class LaunchRing : MonoBehaviour
    {
        public const float Radius = 0.75f;
        public float Life;
        float cooldown, age;
        ShapeView ring, core;

        public static LaunchRing Create(Transform parent, Vector2 pos, float life)
        {
            var go = new GameObject("LaunchRing");
            go.transform.SetParent(parent, false);
            var r = go.AddComponent<LaunchRing>();
            r.ring = ShapeView.Create(go.transform, ShapeType.Circle, Radius, new Color(1f, 0.9f, 0f, 0.1f), Neon.Yellow, 0.5f, 3);
            r.core = ShapeView.Create(go.transform, ShapeType.Circle, 0.2f, Neon.Yellow, Color.white, 0.5f, 4);
            r.Relocate(pos, life);
            return r;
        }

        public void Relocate(Vector2 pos, float life) { transform.position = pos; Life = life; cooldown = 0f; age = 0f; }

        public void Tick(float dt, GameWorld w)
        {
            Life -= dt; age += dt; cooldown -= dt;

            float grow = Mathf.Min(1f, age * 3f);
            ring.transform.localScale = Vector3.one * grow * (1f + 0.08f * Mathf.Sin(Time.time * 6f));
            float a = cooldown > 0f ? 0.3f : Mathf.Clamp01(Life / 1.5f);
            ring.Alpha = a; core.Alpha = a;
            if (cooldown > 0f) return;

            foreach (var p in w.Players)
            {
                if (!p.Alive) continue;
                float reach = Radius + p.Radius * 0.5f;
                if (((Vector2)transform.position - p.Pos).sqrMagnitude < reach * reach) { Trigger(p); break; }
            }
        }

        void Trigger(PlayerController p)
        {
            Vector2 v = p.Velocity;
            Vector2 dir = v.sqrMagnitude > 0.5f ? -v.normalized : -p.Facing;   // standing still: fling backwards from the tip
            p.Launch(dir * Config.LaunchForce);
            Fx.Burst(transform.position, Neon.Yellow, Radius, 2.2f, 0.4f);
            Fx.Sparks(transform.position, Neon.Yellow, 14, 8f, 0.4f);
            App.I.Audio.Play("launch");
            cooldown = 0.8f;
        }
    }
}
