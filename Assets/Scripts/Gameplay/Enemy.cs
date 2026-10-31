using UnityEngine;

namespace TiltRush
{
    public enum EnemyType { Normal, Fast, Big }

    /// <summary>A red dot that hunts the nearest player. Edit the table below to tune / add enemy types.</summary>
    public class Enemy : MonoBehaviour
    {
        struct Def
        {
            public float Speed, Radius; public Color Fill, Line;
            public Def(float s, float r, Color f, Color l) { Speed = s; Radius = r; Fill = f; Line = l; }
        }

        static readonly Def[] defs =
        {
            new Def(2.0f, 0.13f, new Color32(255, 30, 30, 255), Color.white),       // Normal
            new Def(2.8f, 0.095f, new Color32(255, 0, 110, 255), new Color32(255, 150, 200, 255)),  // Fast
            new Def(1.35f, 0.24f, new Color32(150, 0, 0, 255),   new Color32(255, 60, 60, 255)),    // Big
        };

        public EnemyType Type;
        public bool Dead;
        public float Radius;
        public bool Dangerous { get { return warm <= 0f; } }
        public Vector2 Pos { get { return transform.position; } }

        ShapeView view;
        float speed, warm, phase;

        /// <summary>Creates an (inactive) dot. GameWorld keeps killed dots in a pool and reuses them via Setup.</summary>
        public static Enemy Create(Transform parent)
        {
            var go = new GameObject("Enemy");
            go.transform.SetParent(parent, false);
            var e = go.AddComponent<Enemy>();
            var d = defs[0];
            e.view = ShapeView.Create(go.transform, ShapeType.Circle, d.Radius, d.Fill, d.Line, 0.4f, 5);
            return e;
        }

        public void Setup(EnemyType type, Vector2 pos, float speedMul, float phase)
        {
            var d = defs[(int)type];
            gameObject.SetActive(true);
            transform.position = pos;
            Type = type; Dead = false; Radius = d.Radius; speed = d.Speed * speedMul;
            warm = Config.EnemySpawnWarning; this.phase = phase;
            view.SetRadius(d.Radius);
            view.SetColors(d.Fill, d.Line);
            view.Alpha = 0.3f;
        }

        /// <param name="edt">enemy delta time (slow-mo / freeze applied)</param>
        public void Tick(float edt, float dt, GameWorld w)
        {
            if (warm > 0f)
            {
                warm -= dt;
                view.Alpha = warm > 0f ? 0.25f + 0.2f * Mathf.Sin(Time.time * 20f) : 1f;
                return;
            }

            Vector2 targetPos;
            if (!w.NearestTarget(Pos, out targetPos)) return;   // nearest player or decoy

            Vector2 dir = targetPos - Pos;
            float d = dir.magnitude;
            if (d > 0.001f) dir /= d;
            Vector2 perp = new Vector2(-dir.y, dir.x);
            Vector2 move = (dir + perp * Mathf.Sin(w.Elapsed * 2f + phase) * 0.25f).normalized * speed * edt;  // slight weave so swarms spread out
            transform.position = w.PushOut(Pos + move, Radius);   // obstacles block dots too
        }
    }
}
