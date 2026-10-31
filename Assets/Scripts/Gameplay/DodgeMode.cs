using System;
using System.Collections.Generic;
using UnityEngine;

namespace TiltRush
{
    /// <summary>A dot that follows a formula. Motion(t) returns (x, y, scale); scale below 0.3 = hidden and harmless.</summary>
    class FormationDot
    {
        public Func<float, Vector3> Motion;
        public float Delay, Life, Warm, Age, Radius = 0.13f;
        public ShapeView View;
        public Vector2 Pos;
        public bool Lethal;
    }

    /// <summary>
    /// DODGE mode: the dots do not chase you. Instead the director sends patterns - walls with gaps, shrinking rings,
    /// pinwheels, a conga line, a talking smiley, a beating heart, fireworks and the dotted lift...
    /// Every pattern is just a function that adds dots with a movement formula, so new gags are easy to add:
    /// write a method like Smiley() and register it in the constructor.
    /// </summary>
    public class DodgeDirector
    {
        class Ctx { public Vector2 Arena, Half, Player; public float D, Speed; }
        class Entry { public string Name; public float MinDiff; public bool Announce; public Func<Ctx, float> Spawn; }

        static readonly Color DotFill = new Color32(255, 30, 30, 255);
        const float TwoPi = 6.2832f;

        readonly Transform root;
        readonly System.Random rng;
        readonly Action<string, Color> announce;
        readonly List<FormationDot> dots = new List<FormationDot>();
        readonly List<Entry> entries = new List<Entry>();
        readonly Stack<ShapeView> viewPool = new Stack<ShapeView>();   // dot visuals are reused (hundreds per minute)
        float nextIn = 1.2f;
        int last = -1;

        public DodgeDirector(Transform root, System.Random rng, Action<string, Color> announce)
        {
            this.root = root; this.rng = rng; this.announce = announce;

            // Name, difficulty at which the pattern starts to appear, announce it on screen?, spawn function
            entries.Add(new Entry { Name = "RAIN",       MinDiff = 0.00f, Spawn = Rain });
            entries.Add(new Entry { Name = "CURTAIN",    MinDiff = 0.00f, Spawn = Curtain });
            entries.Add(new Entry { Name = "RING",       MinDiff = 0.10f, Spawn = Ring });
            entries.Add(new Entry { Name = "CONGA LINE", MinDiff = 0.15f, Announce = true, Spawn = Conga });
            entries.Add(new Entry { Name = "FLOCK",      MinDiff = 0.20f, Announce = true, Spawn = Flock });
            entries.Add(new Entry { Name = "SMILEY",     MinDiff = 0.25f, Announce = true, Spawn = Smiley });
            entries.Add(new Entry { Name = "FIREWORKS",  MinDiff = 0.30f, Announce = true, Spawn = Fireworks });
            entries.Add(new Entry { Name = "ELEVATOR",   MinDiff = 0.30f, Announce = true, Spawn = Elevator });
            entries.Add(new Entry { Name = "PINWHEEL",   MinDiff = 0.40f, Spawn = Pinwheel });
            entries.Add(new Entry { Name = "HEART",      MinDiff = 0.45f, Announce = true, Spawn = Heart });
            entries.Add(new Entry { Name = "WINDMILL",   MinDiff = 0.55f, Spawn = Windmill });
        }

        // ------------------------------------------------------------------ scheduling
        public void Tick(float dt, float difficulty, Vector2 playerPos)
        {
            UpdateDots(dt);

            nextIn -= dt;
            if (nextIn > 0f || dots.Count > 380) return;

            var c = new Ctx
            {
                Arena = Config.ArenaSize, Half = Config.ArenaSize * 0.5f, Player = playerPos,
                D = difficulty, Speed = 1f + difficulty * 0.8f
            };

            int pick = -1;                                   // a pattern that is unlocked now, and not the same as last time
            for (int tries = 0; tries < 12 && pick < 0; tries++)
            {
                int i = rng.Next(entries.Count);
                if (entries[i].MinDiff <= difficulty && i != last) pick = i;
            }
            if (pick < 0) pick = 0;
            last = pick;

            var e = entries[pick];
            if (e.Announce) announce(e.Name + "!", Color.white);
            float duration = e.Spawn(c);

            // early on we wait until the pattern is nearly over, later patterns overlap
            nextIn = Mathf.Max(1.5f, Mathf.Lerp(duration * 0.9f, duration * 0.35f, difficulty));
        }

        void UpdateDots(float dt)
        {
            for (int i = dots.Count - 1; i >= 0; i--)
            {
                var d = dots[i];
                d.Age += dt;
                if (d.Age < d.Delay) continue;
                float t = d.Age - d.Delay;
                if (t > d.Life)
                {
                    if (d.View != null) { d.View.gameObject.SetActive(false); viewPool.Push(d.View); }
                    dots.RemoveAt(i);
                    continue;
                }

                Vector3 m = d.Motion(t);
                if (d.View == null)
                {
                    if (viewPool.Count > 0) { d.View = viewPool.Pop(); d.View.gameObject.SetActive(true); }
                    else
                    {
                        d.View = ShapeView.Create(root, ShapeType.Circle, d.Radius, DotFill, Color.white, 0.3f, 5);
                        d.View.transform.GetChild(0).gameObject.SetActive(false);   // no glow: saves draw calls with hundreds of dots
                    }
                }
                d.Pos = new Vector2(m.x, m.y);
                d.View.transform.position = d.Pos;
                float pop = t < 0.22f ? Enemy.EaseOutBack(t / 0.22f) : 1f;          // dots pop in
                d.View.transform.localScale = Vector3.one * Mathf.Max(0.0001f, m.z * pop);
                d.View.Alpha = t < d.Warm ? 0.25f + 0.2f * Mathf.Sin(Time.time * 20f) : 1f;
                d.Lethal = t >= d.Warm && m.z > 0.3f;
            }
        }

        public bool Hits(Vector2 p, float r)
        {
            foreach (var d in dots)
            {
                if (!d.Lethal) continue;
                float reach = d.Radius + r;
                if ((d.Pos - p).sqrMagnitude < reach * reach) return true;
            }
            return false;
        }

        // ------------------------------------------------------------------ helpers
        void Add(Func<float, Vector3> motion, float life, float delay = 0f, float warm = 0f)
        {
            dots.Add(new FormationDot { Motion = motion, Life = life, Delay = delay, Warm = warm });
        }

        static Vector3 V(Vector2 p, float scale = 1f) { return new Vector3(p.x, p.y, scale); }
        float R(float a, float b) { return a + (float)rng.NextDouble() * (b - a); }
        static Vector2 Axis(int side) { return side == 0 ? Vector2.right : side == 1 ? Vector2.left : side == 2 ? Vector2.up : Vector2.down; }

        // ------------------------------------------------------------------ patterns (each returns its duration in seconds)

        /// <summary>Scattered drops falling from the top.</summary>
        float Rain(Ctx c)
        {
            int n = 24 + (int)(26 * c.D);
            float longest = 0f;
            for (int i = 0; i < n; i++)
            {
                float x = R(-c.Half.x, c.Half.x), speed = R(3.5f, 6f) * c.Speed, delay = R(0f, 4f);
                float top = c.Half.y + 0.5f, life = (c.Arena.y + 1f) / speed;
                longest = Mathf.Max(longest, delay + life);
                Add(t => V(new Vector2(x, top - speed * t)), life, delay);
            }
            return longest;
        }

        /// <summary>A wall of dots sweeping across the arena with gaps to slip through.</summary>
        float Curtain(Ctx c)
        {
            int side = rng.Next(4);
            Vector2 dir = Axis(side);
            Vector2 nrm = new Vector2(-dir.y, dir.x);
            bool alongX = side < 2;
            float span = alongX ? c.Arena.x : c.Arena.y;
            float lat = alongX ? c.Arena.y : c.Arena.x;
            float speed = 3.4f * c.Speed;
            int gaps = c.D < 0.6f ? 2 : 1;
            float gapW = Mathf.Lerp(3.2f, 2.0f, c.D);

            var centers = new float[gaps];
            for (int k = 0; k < gaps; k++) centers[k] = -lat * 0.5f + lat * (k + 0.2f + 0.6f * (float)rng.NextDouble()) / gaps;

            Vector2 start = -dir * (span * 0.5f + 0.6f);
            float life = (span + 1.2f) / speed;
            for (float u = -lat * 0.5f - 0.5f; u <= lat * 0.5f + 0.5f; u += 0.5f)
            {
                bool inGap = false;
                for (int k = 0; k < gaps; k++) if (Mathf.Abs(u - centers[k]) < gapW * 0.5f) inGap = true;
                if (inGap) continue;
                float off = u;
                Add(t => V(start + dir * (speed * t) + nrm * off), life);
            }
            return life;
        }

        /// <summary>A ring closes in around you - leave through the opening!</summary>
        float Ring(Ctx c)
        {
            Vector2 center = new Vector2(Mathf.Clamp(c.Player.x, -c.Half.x + 3f, c.Half.x - 3f),
                                         Mathf.Clamp(c.Player.y, -c.Half.y + 2f, c.Half.y - 2f));
            const int n = 30;
            float r0 = 5f, dur = 3.8f / Mathf.Sqrt(c.Speed);
            float spin = (rng.Next(2) == 0 ? 1f : -1f) * 0.35f;
            float gapAngle = R(0f, TwoPi);
            int gapDots = c.D < 0.5f ? 4 : 3;
            for (int i = gapDots; i < n; i++)
            {
                float a0 = gapAngle + i / (float)n * TwoPi;
                Add(t =>
                {
                    float r = Mathf.Lerp(r0, 0.3f, Mathf.Clamp01(t / dur));
                    float a = a0 + spin * t;
                    return V(center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r);
                }, dur + 0.2f, 0f, 0.9f);
            }
            return dur + 1f;
        }

        /// <summary>A rotating emitter shoots curved arms of dots.</summary>
        float Pinwheel(Ctx c)
        {
            Vector2 center;
            int guard = 0;
            do
            {
                center = new Vector2(R(-c.Half.x * 0.5f, c.Half.x * 0.5f), R(-c.Half.y * 0.5f, c.Half.y * 0.5f));
            } while ((center - c.Player).magnitude < 4f && ++guard < 8);

            int arms = 3 + (c.D > 0.6f ? 1 : 0) + (c.D > 0.85f ? 1 : 0);
            float spin = (rng.Next(2) == 0 ? 1f : -1f) * (0.9f + 0.5f * c.D);
            float speed = 3.2f * c.Speed, emit = 0.17f;
            const int count = 26;
            float life = (c.Half.magnitude + 1f) / speed;
            for (int a = 0; a < arms; a++)
                for (int k = 0; k < count; k++)
                {
                    float delay = k * emit;
                    float ang = a * TwoPi / arms + spin * delay;
                    Vector2 d = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
                    Add(t => V(center + d * (speed * t)), life, delay, 0.4f);
                }
            return count * emit + 2f;
        }

        /// <summary>Rotating spokes. The hub in the middle is safe.</summary>
        float Windmill(Ctx c)
        {
            Vector2 c0 = new Vector2(R(-c.Half.x * 0.6f, c.Half.x * 0.6f), R(-c.Half.y * 0.3f, c.Half.y * 0.3f));
            int spokes = 2 + (c.D > 0.65f ? 1 : 0) + (c.D > 0.85f ? 1 : 0);
            float spin = (rng.Next(2) == 0 ? 1f : -1f) * (0.7f + 0.5f * c.D);
            float theta0 = R(0f, TwoPi), reach = c.Half.y * 1.1f, life = 7.5f;
            for (int s = 0; s < spokes; s++)
            {
                float baseAng = theta0 + s * TwoPi / spokes;
                for (float rr = 1.1f; rr < reach; rr += 0.55f)
                {
                    float radius = rr;
                    Add(t =>
                    {
                        float ang = baseAng + spin * t;
                        Vector2 drift = new Vector2(0.5f * Mathf.Sin(0.7f * t), 0.3f * Mathf.Cos(0.5f * t));
                        return V(c0 + drift + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * radius);
                    }, life, 0f, 1.0f);
                }
            }
            return life;
        }

        /// <summary>GAG: a long conga line of dots wiggles through the arena.</summary>
        float Conga(Ctx c)
        {
            float sign = rng.Next(2) == 0 ? 1f : -1f;
            float v = 4f * c.Speed, y0 = R(-c.Half.y * 0.4f, c.Half.y * 0.4f);
            float amp = Mathf.Max(0.5f, Mathf.Min(c.Half.y - 1.2f, 2.6f)), w = 1.2f, ph = R(0f, TwoPi);
            float life = (c.Arena.x + 2f) / v;
            const int n = 22;
            int lines = c.D > 0.5f ? 2 : 1;
            for (int line = 0; line < lines; line++)
            {
                float s = line == 0 ? sign : -sign;
                float phase = ph + line * 2.2f;
                float sx = -s * (c.Half.x + 1f);
                float yy = line == 0 ? y0 : -y0;
                for (int i = 0; i < n; i++)
                    Add(t => V(new Vector2(sx + s * v * t, yy + amp * Mathf.Sin(w * t + phase))), life, i * 0.55f / v);
            }
            return life + n * 0.55f / v;
        }

        /// <summary>GAG: a flock of dots flies by in V formation.</summary>
        float Flock(Ctx c)
        {
            float ang = rng.Next(8) * 45f * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
            Vector2 nrm = new Vector2(-dir.y, dir.x);
            float l0 = c.Half.magnitude + 3f, speed = 5.2f * c.Speed;
            float lateral = R(-c.Half.y * 0.6f, c.Half.y * 0.6f);
            float life = 2f * l0 / speed;
            Vector2 start = -dir * l0 + nrm * lateral;
            for (int k = 0; k < 9; k++)
                for (int side = -1; side <= 1; side += 2)
                {
                    if (k == 0 && side == 1) continue;                    // the tip only once
                    Vector2 off = -dir * (k * 0.6f) + nrm * (side * k * 0.4f);
                    Add(t => V(start + dir * (speed * t) + off), life);
                }
            return life * 0.7f;
        }

        /// <summary>GAG: a smiley face rolls through. It blinks and talks. Its inside is a safe spot.</summary>
        float Smiley(Ctx c)
        {
            const float rad = 2.3f;
            int side = rng.Next(4);
            Vector2 dir = Axis(side);
            Vector2 nrm = new Vector2(-dir.y, dir.x);
            bool alongX = side < 2;
            float span = alongX ? c.Arena.x : c.Arena.y, lat = alongX ? c.Arena.y : c.Arena.x;
            float speed = 2.0f * c.Speed, offset = R(-lat * 0.5f + rad, lat * 0.5f - rad);
            Vector2 start = -dir * (span * 0.5f + rad + 0.5f) + nrm * offset;
            float life = (span + 2f * rad + 1f) / speed;
            Func<float, Vector2> centre = t => start + dir * (speed * t) + new Vector2(0f, 0.4f * Mathf.Sin(2f * t));   // bobbing

            for (int i = 0; i < 26; i++)                                    // face outline
            {
                float a = i / 26f * TwoPi;
                Vector2 off = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * rad;
                Add(t => V(centre(t) + off), life);
            }
            foreach (float ex in new[] { -0.85f, 0.85f })                   // eyes (they blink)
                foreach (float ey in new[] { 0.5f, 0.95f })
                {
                    float x = ex, y = ey;
                    Add(t => V(centre(t) + new Vector2(x, y), Mathf.Repeat(t + x, 1.8f) < 0.16f ? 0f : 1f), life);
                }
            for (int m = 0; m < 9; m++)                                     // mouth (talks)
            {
                float ang = Mathf.Lerp(200f, 340f, m / 8f) * Mathf.Deg2Rad;
                Add(t =>
                {
                    float rr = 1.45f + 0.25f * Mathf.Sin(3f * t);
                    return V(centre(t) + new Vector2(Mathf.Cos(ang) * rr, Mathf.Sin(ang) * rr + 0.25f));
                }, life);
            }
            return life * 0.8f;
        }

        /// <summary>GAG: a beating heart drifts across the arena.</summary>
        float Heart(Ctx c)
        {
            const float size = 2.6f;
            Vector2 dir = new Vector2(rng.Next(2) == 0 ? 1f : -1f, rng.Next(2) == 0 ? 1f : -1f).normalized;
            Vector2 nrm = new Vector2(-dir.y, dir.x);
            float l0 = c.Half.magnitude + size + 0.5f, speed = 2.4f * c.Speed;
            Vector2 start = -dir * l0 + nrm * R(-3f, 3f);
            float life = 2f * l0 / speed;
            for (int i = 0; i < 36; i++)
            {
                float u = i / 36f * TwoPi;
                float hx = 16f * Mathf.Pow(Mathf.Sin(u), 3f);
                float hy = 13f * Mathf.Cos(u) - 5f * Mathf.Cos(2f * u) - 2f * Mathf.Cos(3f * u) - Mathf.Cos(4f * u);
                Vector2 off = new Vector2(hx, hy) / 17f * size;
                Add(t => V(start + dir * (speed * t) + off * (1f + 0.12f * Mathf.Sin(5f * t))), life);
            }
            return life * 0.8f;
        }

        /// <summary>Light red rings show where fireworks will burst.</summary>
        float Fireworks(Ctx c)
        {
            int count = 2 + (c.D > 0.5f ? 1 : 0) + (c.D > 0.8f ? 1 : 0);
            const float warn = 1.3f;
            for (int f = 0; f < count; f++)
            {
                Vector2 pos = new Vector2(R(-c.Half.x + 1.5f, c.Half.x - 1.5f), R(-c.Half.y + 1.5f, c.Half.y - 1.5f));
                Fx.Burst(pos, Sword.Marker, 0.3f, 1.3f, warn);
                int n = 16 + (int)(8 * c.D);
                float speed = 4.2f * c.Speed, spinOff = R(0f, TwoPi);
                for (int i = 0; i < n; i++)
                {
                    float a = spinOff + i / (float)n * TwoPi;
                    Vector2 d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                    Add(t => V(pos + d * (speed * t)), 2.4f, warn);
                }
            }
            return warn + 2.4f;
        }

        /// <summary>
        /// GAG (from the original): a lift made of dots. A wall of dots sweeps across the arena, but it has a
        /// doorway with a pipe around it that goes up and down like an elevator. Step into the pipe, ride the doorway
        /// up and down while the wall passes, and get released on the other side.
        /// </summary>
        float Elevator(Ctx c)
        {
            float sign = rng.Next(2) == 0 ? 1f : -1f;
            float speed = 2.6f * c.Speed, gapH = 2.0f, pipeLen = 4.5f, s = 0.5f;
            float amp = Mathf.Max(0.5f, Mathf.Min(c.Half.y - gapH * 0.5f - 0.8f, 2.0f + 1.5f * c.D));
            float phase = R(0f, TwoPi), omega = 1.1f;
            float x0 = -sign * (c.Half.x + pipeLen * 0.5f + 0.5f);
            float life = (c.Arena.x + pipeLen + 1f) / speed;
            Func<float, Vector2> lift = t => new Vector2(x0 + sign * speed * t, amp * Mathf.Sin(omega * t + phase));   // centre of the doorway

            int stack = Mathf.CeilToInt((c.Half.y + amp + 1.5f) / s);       // the wall above and below the doorway
            for (int k = 0; k < stack; k++)
            {
                float dy = gapH * 0.5f + k * s;
                Add(t => V(lift(t) + new Vector2(0f, dy)), life);
                Add(t => V(lift(t) + new Vector2(0f, -dy)), life);
            }
            int jMax = Mathf.RoundToInt(pipeLen * 0.5f / s);                // the pipe: two rows of dots left and right of the doorway
            for (int j = -jMax; j <= jMax; j++)
            {
                if (j == 0) continue;
                float dx = j * s;
                Add(t => V(lift(t) + new Vector2(dx, gapH * 0.5f)), life);
                Add(t => V(lift(t) + new Vector2(dx, -gapH * 0.5f)), life);
            }
            return life * 0.85f;
        }
    }
}
