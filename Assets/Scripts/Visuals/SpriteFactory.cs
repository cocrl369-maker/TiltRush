using System;
using System.Collections.Generic;
using UnityEngine;

namespace TiltRush
{
    /// <summary>Pictograms on the pickups: you can see roughly what a gadget does.</summary>
    public enum GadgetIcon { Clock, Snowflake, Shield, Shockwave, Nuke, Crosshair, Lance, Bolt, Turret, Ghost, Shrink, Boost, Vortex, Orbit }

    public enum ShapeType
    {
        Circle, Square, Triangle, Diamond, Arrow, Dart, Kite, Ship, Needle, Comet, Wing, Falcon,
        Saw, Spike6, Spike10,  // enemies: saw blade, small spiky star, big spiky ball
        Rocket, Star, Crystal, Fang, Manta, Bolt, Spear, Hornet, Crown, Saber, Phoenix, Nova   // unlockable skins
    }

    /// <summary>
    /// Generates all sprites in code (white, tinted later via SpriteRenderer.color) -
    /// no art assets needed. Add a new shape: add an enum entry + a case in Sdf().
    /// </summary>
    public static class SpriteFactory
    {
        const int Res = 128;
        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();
        delegate float AlphaFn(float x, float y);   // x,y in -1..1

        public static Sprite Fill(ShapeType s) { return Cached("fill" + s, () => Shape(s, false)); }
        public static Sprite Outline(ShapeType s) { return Cached("line" + s, () => Shape(s, true)); }
        public static Sprite Pixel() { return Cached("pixel", () => Make(4, 4, (x, y) => 1f)); }
        public static Sprite Glow()
        {
            return Cached("glow", () => Make(Res, Res, (x, y) =>
                Mathf.Pow(Mathf.Clamp01(1f - Mathf.Sqrt(x * x + y * y)), 2.2f)));
        }
        public static Sprite SoftLine()
        {
            return Cached("softline", () => Make(64, 4, (x, y) => Mathf.Pow(1f - Mathf.Abs(x), 2f)));
        }

        /// <summary>Soft band: fades out across its height (used for glowing lines).</summary>
        public static Sprite SoftBand()
        {
            return Cached("softband", () => Make(64, 64, (x, y) => Mathf.Pow(1f - Mathf.Abs(y), 2f)));
        }

        /// <summary>Vertical gradient: transparent at the bottom, opaque at the top (background).</summary>
        public static Sprite VGradient()
        {
            return Cached("vgrad", () => Make(64, 64, (x, y) => { float t = (y + 1f) * 0.5f; return t * t * (3f - 2f * t); }));
        }

        /// <summary>Background block: rounded square outline with a smaller square inside (GD block look).</summary>
        public static Sprite TileOutline()
        {
            return Cached("tileline", () => Make(Res, Res, (x, y) =>
            {
                float outer = RoundBox(x, y, 0.86f, 0.12f), inner = RoundBox(x, y, 0.42f, 0.06f);
                float a1 = Mathf.Clamp01(0.5f - (Mathf.Abs(outer) - 0.035f) / 0.02f);
                float a2 = Mathf.Clamp01(0.5f - (Mathf.Abs(inner) - 0.025f) / 0.02f) * 0.7f;
                return Mathf.Max(a1, a2);
            }));
        }

        /// <summary>Background block: two rows of bricks (lava, desert).</summary>
        public static Sprite BrickOutline()
        {
            return Cached("bricks", () => Make(Res, Res, (x, y) =>
            {
                float d = Mathf.Min(Mathf.Abs(y), Mathf.Min(Mathf.Abs(y - 0.94f), Mathf.Abs(y + 0.94f)));       // mortar lines
                if (y > 0f) d = Mathf.Min(d, Mathf.Min(Mathf.Abs(x), Mathf.Abs(Mathf.Abs(x) - 0.94f)));       // joints, top row
                else d = Mathf.Min(d, Mathf.Abs(Mathf.Abs(x) - 0.5f));                                        // joints, bottom row (offset)
                return Mathf.Clamp01(0.5f - (d - 0.035f) / 0.02f);
            }));
        }

        /// <summary>Background block: filled rounded square.</summary>
        public static Sprite TileFill()
        {
            return Cached("tilefill", () => Make(Res, Res, (x, y) => Mathf.Clamp01(0.5f - RoundBox(x, y, 0.9f, 0.12f) / 0.02f)));
        }

        /// <summary>Two angry slanted eyes (big enemies). Fits -1..1, eyes in the middle band.</summary>
        public static Sprite AngryEyes()
        {
            return Cached("eyes", () => Make(Res, Res, (x, y) =>
            {
                float d = Mathf.Min(Poly(x, y, EyeL), Poly(-x, y, EyeL));
                return Mathf.Clamp01(0.5f - d / 0.03f);
            }));
        }
        static readonly Vector2[] EyeL = { new Vector2(-0.82f, 0.32f), new Vector2(-0.12f, 0.02f), new Vector2(-0.18f, -0.32f), new Vector2(-0.72f, -0.22f) };

        static float RoundBox(float x, float y, float half, float r)
        {
            float qx = Mathf.Abs(x) - (half - r), qy = Mathf.Abs(y) - (half - r);
            return new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;
        }

        /// <summary>Dark edges, transparent centre - gives the menus depth.</summary>
        public static Sprite Vignette()
        {
            return Cached("vignette", () => Make(128, 128, (x, y) =>
            {
                float r = Mathf.Sqrt(x * x + y * y) / 1.414f;
                float t = Mathf.Clamp01((r - 0.45f) / 0.55f);
                return t * t * (3f - 2f * t) * 0.7f;
            }));
        }

        /// <summary>Rounded rectangle, 9-sliced (use Image.Type.Sliced). Used for the buttons.</summary>
        public static Sprite RoundedFill() { return Cached("rfill", () => Rounded(false)); }
        public static Sprite RoundedRing() { return Cached("rring", () => Rounded(true)); }
        /// <summary>Thick dark outline for the GD-style buttons.</summary>
        public static Sprite RoundedRingThick() { return Cached("rring2", () => Rounded(true, 12f)); }
        /// <summary>Rounded fill that fades towards the bottom (sheen on buttons).</summary>
        public static Sprite RoundedSheen() { return Cached("rsheen", () => Rounded(false, 7f, true)); }

        /// <summary>Soft glow around a rounded rectangle, 9-sliced (button / panel glow).</summary>
        public static Sprite RoundedGlow()
        {
            return Cached("rglow", () =>
            {
                const int n = 128;
                const float radius = 30f, pad = 26f;
                float half = n * 0.5f - pad;
                var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
                var pixels = new Color32[n * n];
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        float qx = Mathf.Abs(x + 0.5f - n * 0.5f) - (half - radius);
                        float qy = Mathf.Abs(y + 0.5f - n * 0.5f) - (half - radius);
                        float d = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude + Mathf.Min(Mathf.Max(qx, qy), 0f) - radius;
                        float a = d <= 0f ? 1f : Mathf.Exp(-d / 8f) * (1f - d / pad);
                        pixels[y * n + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(a) * 255f));
                    }
                tex.SetPixels32(pixels); tex.Apply();
                tex.wrapMode = TextureWrapMode.Clamp; tex.filterMode = FilterMode.Bilinear;
                return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(60, 60, 60, 60));
            });
        }

        /// <summary>Sword sprite, 1 unit long, tip points up (+y).</summary>
        public static Sprite BladeFill() { return Cached("bfill", () => Blade(false)); }
        public static Sprite BladeLine() { return Cached("bline", () => Blade(true)); }

        static Sprite Cached(string key, Func<Sprite> make)
        {
            if (!cache.TryGetValue(key, out var s) || s == null) { s = make(); cache[key] = s; }
            return s;
        }

        // Signed distance (negative = inside), shapes fit inside -1..1
        static float Sdf(ShapeType s, float x, float y)
        {
            switch (s)
            {
                case ShapeType.Circle: return Mathf.Sqrt(x * x + y * y) - 0.9f;
                case ShapeType.Square: return Mathf.Max(Mathf.Abs(x), Mathf.Abs(y)) - 0.8f;
                case ShapeType.Arrow: return Poly(x, y, ArrowV);
                case ShapeType.Dart:  return Poly(x, y, DartV);
                case ShapeType.Kite:  return Poly(x, y, KiteV);
                case ShapeType.Ship:  return Poly(x, y, ShipV);
                case ShapeType.Needle: return Poly(x, y, NeedleV);
                case ShapeType.Comet: return Poly(x, y, CometV);
                case ShapeType.Wing:  return Poly(x, y, WingV);
                case ShapeType.Falcon: return Poly(x, y, FalconV);
                case ShapeType.Rocket: return Poly(x, y, RocketV);
                case ShapeType.Star:   return Poly(x, y, StarV);
                case ShapeType.Crystal: return Poly(x, y, CrystalV);
                case ShapeType.Fang:   return Poly(x, y, FangV);
                case ShapeType.Manta:  return Poly(x, y, MantaV);
                case ShapeType.Bolt:   return Poly(x, y, BoltV);
                case ShapeType.Spear:  return Poly(x, y, SpearV);
                case ShapeType.Hornet: return Poly(x, y, HornetV);
                case ShapeType.Crown:  return Poly(x, y, CrownV);
                case ShapeType.Saber:  return Poly(x, y, SaberV);
                case ShapeType.Phoenix: return Poly(x, y, PhoenixV);
                case ShapeType.Nova:   return Poly(x, y, NovaV);
                case ShapeType.Diamond: return (Mathf.Abs(x) + Mathf.Abs(y) - 0.95f) * 0.7071f;
                case ShapeType.Saw:
                {
                    // 8 asymmetric teeth -> looks like a turning saw blade
                    float u = Mathf.Repeat(Mathf.Atan2(y, x) / 6.2832f * 8f, 1f);
                    return (Mathf.Sqrt(x * x + y * y) - (0.64f + 0.26f * (1f - u))) * 0.8f;
                }
                case ShapeType.Spike6:
                case ShapeType.Spike10:
                {
                    float n = s == ShapeType.Spike6 ? 6f : 10f;
                    float u = Mathf.Repeat(Mathf.Atan2(y, x) / 6.2832f * n, 1f);
                    float tri = 1f - Mathf.Abs(u * 2f - 1f);           // 0 between spikes, 1 at a spike tip
                    return (Mathf.Sqrt(x * x + y * y) - (0.6f + 0.32f * tri * tri)) * 0.75f;
                }
                default: // Triangle
                    float d1 = -y - 0.6f;
                    float d2 = (-1.5f * (x + 0.85f) + 0.85f * (y + 0.6f)) / 1.724f;
                    float d3 = (1.5f * (x - 0.85f) + 0.85f * (y + 0.6f)) / 1.724f;
                    return Mathf.Max(d1, Mathf.Max(d2, d3));
            }
        }

        // Pointed player shapes - the tip is at +y (up) and gets rotated towards the movement direction.
        static readonly Vector2[] ArrowV = { new Vector2(0, 0.95f), new Vector2(0.75f, -0.85f), new Vector2(-0.75f, -0.85f) };
        static readonly Vector2[] DartV  = { new Vector2(0, 0.95f), new Vector2(0.75f, -0.85f), new Vector2(0, -0.4f), new Vector2(-0.75f, -0.85f) };
        static readonly Vector2[] KiteV  = { new Vector2(0, 0.95f), new Vector2(0.6f, -0.1f), new Vector2(0, -0.85f), new Vector2(-0.6f, -0.1f) };
        static readonly Vector2[] NeedleV = { new Vector2(0, 0.98f), new Vector2(0.32f, -0.85f), new Vector2(0, -0.55f), new Vector2(-0.32f, -0.85f) };
        static readonly Vector2[] CometV = { new Vector2(0, 0.95f), new Vector2(0.4f, 0.3f), new Vector2(0.62f, -0.2f), new Vector2(0.5f, -0.6f), new Vector2(0.2f, -0.85f),
                                             new Vector2(-0.2f, -0.85f), new Vector2(-0.5f, -0.6f), new Vector2(-0.62f, -0.2f), new Vector2(-0.4f, 0.3f) };
        static readonly Vector2[] WingV  = { new Vector2(0, 0.9f), new Vector2(0.9f, -0.7f), new Vector2(0.3f, -0.5f), new Vector2(0, -0.85f), new Vector2(-0.3f, -0.5f), new Vector2(-0.9f, -0.7f) };
        static readonly Vector2[] FalconV = { new Vector2(0, 0.95f), new Vector2(0.55f, 0.1f), new Vector2(0.95f, -0.75f), new Vector2(0.35f, -0.45f), new Vector2(0, -0.85f),
                                              new Vector2(-0.35f, -0.45f), new Vector2(-0.95f, -0.75f), new Vector2(-0.55f, 0.1f) };
        static readonly Vector2[] ShipV  = { new Vector2(0, 0.95f), new Vector2(0.8f, -0.3f), new Vector2(0.5f, -0.85f), new Vector2(-0.5f, -0.85f), new Vector2(-0.8f, -0.3f) };

        // Unlockable skins (tip up as well)
        static readonly Vector2[] RocketV = { new Vector2(0, 0.95f), new Vector2(0.32f, 0.45f), new Vector2(0.32f, -0.35f), new Vector2(0.62f, -0.85f), new Vector2(0.15f, -0.6f),
                                              new Vector2(-0.15f, -0.6f), new Vector2(-0.62f, -0.85f), new Vector2(-0.32f, -0.35f), new Vector2(-0.32f, 0.45f) };
        static readonly Vector2[] StarV = StarPoly(5, 0.95f, 0.42f, 0f);
        static readonly Vector2[] NovaV = StarPoly(8, 0.95f, 0.5f, 0f);
        static readonly Vector2[] CrystalV = { new Vector2(0, 0.95f), new Vector2(0.5f, 0.35f), new Vector2(0.5f, -0.35f), new Vector2(0, -0.95f), new Vector2(-0.5f, -0.35f), new Vector2(-0.5f, 0.35f) };
        static readonly Vector2[] FangV = { new Vector2(0, 0.95f), new Vector2(0.48f, -0.15f), new Vector2(0.32f, -0.85f), new Vector2(0, -0.5f), new Vector2(-0.32f, -0.85f), new Vector2(-0.48f, -0.15f) };
        static readonly Vector2[] MantaV = { new Vector2(0, 0.9f), new Vector2(0.35f, 0.35f), new Vector2(0.95f, -0.1f), new Vector2(0.4f, -0.3f), new Vector2(0.12f, -0.9f), new Vector2(0, -0.6f),
                                             new Vector2(-0.12f, -0.9f), new Vector2(-0.4f, -0.3f), new Vector2(-0.95f, -0.1f), new Vector2(-0.35f, 0.35f) };
        static readonly Vector2[] BoltV = { new Vector2(0.28f, 0.95f), new Vector2(-0.68f, -0.12f), new Vector2(-0.06f, -0.12f), new Vector2(-0.3f, -0.95f), new Vector2(0.7f, 0.22f), new Vector2(0.08f, 0.22f) };
        static readonly Vector2[] SpearV = { new Vector2(0, 0.98f), new Vector2(0.52f, 0.25f), new Vector2(0.26f, 0.22f), new Vector2(0.26f, -0.9f), new Vector2(-0.26f, -0.9f), new Vector2(-0.26f, 0.22f), new Vector2(-0.52f, 0.25f) };
        static readonly Vector2[] HornetV = { new Vector2(0, 0.95f), new Vector2(0.2f, 0.55f), new Vector2(0.85f, 0.35f), new Vector2(0.3f, 0f), new Vector2(0.35f, -0.6f), new Vector2(0, -0.9f),
                                              new Vector2(-0.35f, -0.6f), new Vector2(-0.3f, 0f), new Vector2(-0.85f, 0.35f), new Vector2(-0.2f, 0.55f) };
        static readonly Vector2[] CrownV = { new Vector2(0, 0.95f), new Vector2(0.25f, 0.4f), new Vector2(0.72f, 0.7f), new Vector2(0.6f, -0.3f), new Vector2(0.75f, -0.85f),
                                             new Vector2(-0.75f, -0.85f), new Vector2(-0.6f, -0.3f), new Vector2(-0.72f, 0.7f), new Vector2(-0.25f, 0.4f) };
        static readonly Vector2[] SaberV = { new Vector2(0, 0.98f), new Vector2(0.3f, -0.3f), new Vector2(0.75f, -0.45f), new Vector2(0.2f, -0.6f), new Vector2(0, -0.95f),
                                             new Vector2(-0.2f, -0.6f), new Vector2(-0.75f, -0.45f), new Vector2(-0.3f, -0.3f) };
        static readonly Vector2[] PhoenixV = { new Vector2(0, 0.95f), new Vector2(0.25f, 0.4f), new Vector2(0.95f, 0.55f), new Vector2(0.55f, 0f), new Vector2(0.8f, -0.5f), new Vector2(0.25f, -0.35f),
                                               new Vector2(0, -0.9f), new Vector2(-0.25f, -0.35f), new Vector2(-0.8f, -0.5f), new Vector2(-0.55f, 0f), new Vector2(-0.95f, 0.55f), new Vector2(-0.25f, 0.4f) };

        static Vector2[] StarPoly(int points, float outer, float inner, float rot)
        {
            var v = new Vector2[points * 2];
            for (int i = 0; i < v.Length; i++)
            {
                float a = Mathf.PI * 0.5f + rot + i * Mathf.PI / points;
                float r = i % 2 == 0 ? outer : inner;
                v[i] = new Vector2(Mathf.Cos(a) * r, Mathf.Sin(a) * r);
            }
            return v;
        }

        /// <summary>Second-colour detail of a skin: a smaller copy of the shape (Inner) or a round cockpit (Core).</summary>
        public static Sprite Detail(ShapeType s, SkinDetail kind)
        {
            if (kind == SkinDetail.None) return null;
            return Cached("detail" + s + kind, () => Make(Res, Res, (x, y) =>
            {
                float d = kind == SkinDetail.Inner
                    ? Sdf(s, x / 0.45f, (y + 0.05f) / 0.45f) * 0.45f
                    : Mathf.Sqrt(x * x + (y + 0.05f) * (y + 0.05f)) - 0.2f;
                return Mathf.Clamp01(0.5f - d / 0.03f);
            }));
        }

        /// <summary>White pictogram for a gadget (drawn on top of the round pickup token).</summary>
        public static Sprite Gadget(GadgetIcon icon)
        {
            return Cached("gadget" + icon, () => Make(Res, Res, (x, y) => Mathf.Clamp01(0.5f - GadgetSdf(icon, x, y) / 0.035f)));
        }

        static float Seg(float px, float py, float ax, float ay, float bx, float by, float w)
        {
            float pax = px - ax, pay = py - ay, bax = bx - ax, bay = by - ay;
            float h = Mathf.Clamp01((pax * bax + pay * bay) / (bax * bax + bay * bay));
            float dx = pax - bax * h, dy = pay - bay * h;
            return Mathf.Sqrt(dx * dx + dy * dy) - w * 0.5f;
        }

        static float Ring(float x, float y, float r, float w) { return Mathf.Abs(Mathf.Sqrt(x * x + y * y) - r) - w * 0.5f; }
        static float Disc(float x, float y, float cx, float cy, float r) { return Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) - r; }

        static readonly Vector2[] ShieldV = { new Vector2(-0.7f, 0.75f), new Vector2(0f, 0.92f), new Vector2(0.7f, 0.75f), new Vector2(0.66f, 0.05f),
                                              new Vector2(0.4f, -0.5f), new Vector2(0f, -0.9f), new Vector2(-0.4f, -0.5f), new Vector2(-0.66f, 0.05f) };
        static readonly Vector2[] GadgetBoltV = { new Vector2(0.25f, 0.92f), new Vector2(-0.55f, -0.08f), new Vector2(-0.05f, -0.08f), new Vector2(-0.25f, -0.92f), new Vector2(0.58f, 0.15f), new Vector2(0.06f, 0.15f) };

        static float GadgetSdf(GadgetIcon icon, float x, float y)
        {
            switch (icon)
            {
                case GadgetIcon.Clock:        // Slow-Mo: clock
                    return Mathf.Min(Ring(x, y, 0.72f, 0.16f),
                           Mathf.Min(Seg(x, y, 0, 0, 0, 0.48f, 0.14f), Mathf.Min(Seg(x, y, 0, 0, 0.36f, -0.12f, 0.14f), Disc(x, y, 0, 0, 0.12f))));
                case GadgetIcon.Snowflake:    // Freeze: snowflake
                {
                    float d = 9f;
                    for (int k = 0; k < 6; k++)
                    {
                        float a = k * Mathf.PI / 3f + Mathf.PI / 2f, ca = Mathf.Cos(a), sa = Mathf.Sin(a);
                        d = Mathf.Min(d, Seg(x, y, 0, 0, ca * 0.85f, sa * 0.85f, 0.13f));
                        float bx = ca * 0.5f, by = sa * 0.5f;          // little branches
                        for (int s = -1; s <= 1; s += 2)
                        {
                            float b = a + s * 0.7f;
                            d = Mathf.Min(d, Seg(x, y, bx, by, bx + Mathf.Cos(b) * 0.28f, by + Mathf.Sin(b) * 0.28f, 0.1f));
                        }
                    }
                    return d;
                }
                case GadgetIcon.Shield:       // Shield
                    return Mathf.Max(Poly(x, y, ShieldV), -Mathf.Max(Poly(x / 0.6f, (y - 0.04f) / 0.6f, ShieldV) * 0.6f, -(Mathf.Abs(x) - 0.06f)));
                case GadgetIcon.Shockwave:    // Shockwave: rings
                    return Mathf.Min(Disc(x, y, 0, 0, 0.2f), Mathf.Min(Ring(x, y, 0.5f, 0.12f), Ring(x, y, 0.82f, 0.1f)));
                case GadgetIcon.Nuke:         // Nuke: radiation sign
                {
                    float r = Mathf.Sqrt(x * x + y * y);
                    float a = Mathf.Repeat(Mathf.Atan2(y, x) - Mathf.PI / 2f + Mathf.PI / 3f, 2f * Mathf.PI / 3f) - Mathf.PI / 3f;   // 3 blades, one points up
                    float blade = Mathf.Max(Mathf.Max(0.3f - r, r - 0.88f), (Mathf.Abs(a) - Mathf.PI / 6f) * r);
                    return Mathf.Min(blade, Disc(x, y, 0, 0, 0.17f));
                }
                case GadgetIcon.Crosshair:    // Seekers: target crosshair
                    return Mathf.Min(Ring(x, y, 0.55f, 0.13f), Mathf.Min(Disc(x, y, 0, 0, 0.13f),
                           Mathf.Min(Mathf.Min(Seg(x, y, 0, 0.35f, 0, 0.92f, 0.13f), Seg(x, y, 0, -0.35f, 0, -0.92f, 0.13f)),
                                     Mathf.Min(Seg(x, y, 0.35f, 0, 0.92f, 0, 0.13f), Seg(x, y, -0.35f, 0, -0.92f, 0, 0.13f)))));
                case GadgetIcon.Lance:        // Lance: three beams fanning out
                {
                    float d = Disc(x, y, 0, -0.72f, 0.17f);
                    for (int k = -1; k <= 1; k++)
                    {
                        float a = Mathf.PI / 2f + k * 0.45f;
                        d = Mathf.Min(d, Seg(x, y, 0, -0.72f, Mathf.Cos(a) * 1.55f, -0.72f + Mathf.Sin(a) * 1.55f, 0.15f));
                    }
                    return d;
                }
                case GadgetIcon.Bolt:         // Storm: lightning
                    return Poly(x, y, GadgetBoltV);
                case GadgetIcon.Turret:       // Turret: base + barrel
                    return Mathf.Min(Mathf.Min(Disc(x, y, -0.15f, -0.2f, 0.42f), Seg(x, y, -0.15f, -0.2f, 0.7f, 0.55f, 0.24f)),
                                     Seg(x, y, -0.75f, -0.75f, 0.45f, -0.75f, 0.16f));
                case GadgetIcon.Shrink:       // Shrink: four arrows pointing inwards
                {
                    float d = 9f;
                    for (int k = 0; k < 4; k++)
                    {
                        float a = k * Mathf.PI / 2f + Mathf.PI / 4f, ca = Mathf.Cos(a), sa = Mathf.Sin(a);
                        float tx = ca * 0.22f, ty = sa * 0.22f, ox = ca * 0.85f, oy = sa * 0.85f;
                        d = Mathf.Min(d, Seg(x, y, tx, ty, ox, oy, 0.13f));
                        float nx = -sa, ny = ca;                       // arrow head at the inner end
                        d = Mathf.Min(d, Seg(x, y, tx, ty, tx + (ca + nx) * 0.25f, ty + (sa + ny) * 0.25f, 0.13f));
                        d = Mathf.Min(d, Seg(x, y, tx, ty, tx + (ca - nx) * 0.25f, ty + (sa - ny) * 0.25f, 0.13f));
                    }
                    return d;
                }
                case GadgetIcon.Boost:        // Boost: double chevron
                {
                    float d = 9f;
                    for (int k = 0; k < 2; k++)
                    {
                        float ox = -0.42f + k * 0.5f;
                        d = Mathf.Min(d, Mathf.Min(Seg(x, y, ox, 0.6f, ox + 0.42f, 0f, 0.2f), Seg(x, y, ox + 0.42f, 0f, ox, -0.6f, 0.2f)));
                    }
                    return d;
                }
                case GadgetIcon.Vortex:       // Black hole: spiral arms around a dark core
                {
                    float r = Mathf.Sqrt(x * x + y * y), a = Mathf.Atan2(y, x);
                    float arm = Mathf.Repeat(a * 3f / (2f * Mathf.PI) + r * 1.6f, 1f);    // 3 spiral arms
                    float spiral = (Mathf.Abs(arm - 0.5f) - 0.14f) * r * 2.2f;
                    spiral = Mathf.Max(spiral, Mathf.Max(0.28f - r, r - 0.88f));
                    return Mathf.Min(spiral, Ring(x, y, 0.2f, 0.1f));
                }
                case GadgetIcon.Orbit:        // Orbit: core with three moons on a ring
                {
                    float d = Mathf.Min(Disc(x, y, 0, 0, 0.22f), Ring(x, y, 0.62f, 0.07f));
                    for (int k = 0; k < 3; k++)
                    {
                        float a = Mathf.PI / 2f + k * 2f * Mathf.PI / 3f;
                        d = Mathf.Min(d, Disc(x, y, Mathf.Cos(a) * 0.62f, Mathf.Sin(a) * 0.62f, 0.18f));
                    }
                    return d;
                }
                default:                      // Decoy: ghost
                {
                    float body = Mathf.Min(Disc(x, y, 0, 0.12f, 0.6f), Mathf.Max(Mathf.Abs(x) - 0.6f, Mathf.Max(y - 0.12f, -0.72f - y)));
                    float waves = Mathf.Min(Disc(x, y, -0.4f, -0.78f, 0.2f), Mathf.Min(Disc(x, y, 0f, -0.78f, 0.2f), Disc(x, y, 0.4f, -0.78f, 0.2f)));
                    float eyes = Mathf.Min(Disc(x, y, -0.22f, 0.18f, 0.13f), Disc(x, y, 0.22f, 0.18f, 0.13f));
                    return Mathf.Max(Mathf.Max(body, -waves), -eyes);
                }
            }
        }

        /// <summary>Gear icon (options button).</summary>
        public static Sprite Gear()
        {
            return Cached("gear", () => Make(Res, Res, (x, y) =>
            {
                float r = Mathf.Sqrt(x * x + y * y);
                float u = Mathf.Repeat(Mathf.Atan2(y, x) / 6.2832f * 8f, 1f);
                float tooth = Mathf.Abs(u - 0.5f) < 0.22f ? 0.9f : 0.7f;
                float outer = r - tooth, hole = 0.3f - r;
                return Mathf.Clamp01(0.5f - Mathf.Max(outer, hole) / 0.03f);
            }));
        }

        // Signed distance to a polygon (negative inside)
        static float Poly(float px, float py, Vector2[] v)
        {
            int n = v.Length;
            float d = (px - v[0].x) * (px - v[0].x) + (py - v[0].y) * (py - v[0].y);
            float s = 1f;
            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                float ex = v[j].x - v[i].x, ey = v[j].y - v[i].y;
                float wx = px - v[i].x, wy = py - v[i].y;
                float t = Mathf.Clamp01((wx * ex + wy * ey) / (ex * ex + ey * ey));
                float bx = wx - ex * t, by = wy - ey * t;
                d = Mathf.Min(d, bx * bx + by * by);
                bool c1 = py >= v[i].y, c2 = py < v[j].y, c3 = ex * wy > ey * wx;
                if ((c1 && c2 && c3) || (!c1 && !c2 && !c3)) s = -s;
            }
            return s * Mathf.Sqrt(d);
        }

        static Sprite Rounded(bool ring, float thick = 7f, bool fade = false)
        {
            const int n = 128;
            const float radius = 44f;
            float half = n * 0.5f - 1.5f;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            var pixels = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float qx = Mathf.Abs(x + 0.5f - n * 0.5f) - (half - radius);
                    float qy = Mathf.Abs(y + 0.5f - n * 0.5f) - (half - radius);
                    float d = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude + Mathf.Min(Mathf.Max(qx, qy), 0f) - radius;
                    float a = ring ? 0.5f - (Mathf.Abs(d + thick * 0.5f) - thick * 0.5f) : 0.5f - d;
                    if (fade) a *= Mathf.Pow((y + 0.5f) / n, 1.6f);           // bright at the top, gone at the bottom
                    pixels[y * n + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(a) * 255f));
                }
            tex.SetPixels32(pixels);
            tex.Apply();
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f, 0,
                                 SpriteMeshType.FullRect, new Vector4(48, 48, 48, 48));
        }

        // Sword polygons in pixels (64 x 256 texture, tip up)
        static readonly Vector2[] BladeV = { new Vector2(0, 126), new Vector2(13, 92), new Vector2(13, -58), new Vector2(-13, -58), new Vector2(-13, 92) };
        static readonly Vector2[] GuardV = { new Vector2(-28, -58), new Vector2(28, -58), new Vector2(28, -70), new Vector2(-28, -70) };
        static readonly Vector2[] HiltV  = { new Vector2(-6, -70), new Vector2(6, -70), new Vector2(6, -122), new Vector2(-6, -122) };

        static Sprite Blade(bool outline)
        {
            const int w = 64, h = 256;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var pixels = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float px = x + 0.5f - w * 0.5f, py = y + 0.5f - h * 0.5f;
                    float d = Mathf.Min(Poly(px, py, BladeV), Mathf.Min(Poly(px, py, GuardV), Poly(px, py, HiltV)));
                    float a = outline ? 0.5f - (Mathf.Abs(d + 2f) - 2f) : 0.5f - d;
                    pixels[y * w + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(a) * 255f));
                }
            tex.SetPixels32(pixels);
            tex.Apply();
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 256f);
        }

        static Sprite Shape(ShapeType s, bool outline)
        {
            const float aa = 0.03f;      // edge softness
            const float thick = 0.16f;   // outline thickness
            return Make(Res, Res, (x, y) =>
            {
                float d = Sdf(s, x, y);
                if (!outline) return Mathf.Clamp01(0.5f - d / aa);
                float band = Mathf.Abs(d + thick * 0.5f) - thick * 0.5f;
                return Mathf.Clamp01(0.5f - band / aa);
            });
        }

        static Sprite Make(int w, int h, AlphaFn f)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float a = f((x + 0.5f) / w * 2f - 1f, (y + 0.5f) / h * 2f - 1f);
                    px[y * w + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(a) * 255f));
                }
            tex.SetPixels32(px);
            tex.Apply();
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), w);
        }
    }
}
