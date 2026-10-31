using UnityEngine;

namespace TiltRush
{
    public struct ThemeColors
    {
        public Color BgTop, BgBottom;   // background gradient
        public Color Tile;              // light colour for the background blocks
        public Color Accent;            // bright colour: arena border, obstacle outlines
        public Color Dark;              // very dark tint of the theme (enemy bodies, dark blocks)
        public float Hue;               // base hue of the theme 0..1
    }

    /// <summary>
    /// The colour world of the game (Geometry-Dash style: colourful, glowing, but not harsh).
    /// The colours depend on the difficulty: calm turquoise-blue at the start -> blue -> purple -> magenta -> red ->
    /// glowing orange at the end, and they become more saturated ("knalliger") and brighter the harder it gets.
    /// Tune the look here: Hues = the colour journey, the Lerp values = how muted / bright it is.
    /// </summary>
    public static class GameTheme
    {
        /// <summary>Hue for difficulty 0, 0.2, 0.4 ... 1.0 (values above 1 wrap around: 1.08 = orange).</summary>
        static readonly float[] Hues = { 0.53f, 0.62f, 0.75f, 0.87f, 0.99f, 1.075f };

        /// <summary>Smoothed difficulty 0..1 (set every frame by the background).</summary>
        public static float Level { get; private set; } = 0.1f;
        public static ThemeColors Now { get; private set; } = Evaluate(0.1f);

        public static void SetLevel(float level) { Level = Mathf.Clamp01(level); Now = Evaluate(Level); }

        public static ThemeColors Evaluate(float d)
        {
            d = Mathf.Clamp01(d);
            float f = d * (Hues.Length - 1);
            int i = Mathf.Min((int)f, Hues.Length - 2);
            float hue = Mathf.Lerp(Hues[i], Hues[i + 1], f - i);
            float sat = Mathf.Lerp(0.72f, 1f, d);

            ThemeColors t;
            t.BgBottom = HSV(hue, sat, Mathf.Lerp(0.26f, 0.42f, d));
            t.BgTop = HSV(hue + 0.04f, sat * 0.9f, Mathf.Lerp(0.52f, 0.82f, d));
            t.Tile = HSV(hue + 0.02f, sat * 0.45f, 1f);
            t.Accent = HSV(hue - 0.03f, Mathf.Lerp(0.3f, 0.75f, d), 1f);
            t.Dark = HSV(hue, 0.7f, 0.08f);
            t.Hue = Mathf.Repeat(hue, 1f);
            return t;
        }

        static Color HSV(float h, float s, float v) { return Color.HSVToRGB(Mathf.Repeat(h, 1f), Mathf.Clamp01(s), Mathf.Clamp01(v)); }

        // ---------------------------------------------------------------- enemies
        // Dark bodies with glowing rims stay readable on every background colour (like the black spikes in GD).

        public static Color EnemyBody { get { return Color.Lerp(new Color(0.05f, 0.04f, 0.08f, 1f), Now.Dark, 0.5f); } }

        public static Color EnemyRim(EnemyType type)
        {
            float d = Level;
            switch (type)
            {
                case EnemyType.Fast: return Color.Lerp(new Color32(255, 185, 245, 255), new Color32(255, 20, 200, 255), d);
                case EnemyType.Big:  return Color.Lerp(new Color32(255, 215, 170, 255), new Color32(255, 110, 0, 255), d);
                default:             return Color.Lerp(new Color32(255, 200, 205, 255), new Color32(255, 35, 70, 255), d);
            }
        }

        public static Color EnemyEye { get { return Color.Lerp(new Color32(255, 250, 200, 255), new Color32(255, 230, 0, 255), Level); } }
        public static float EnemyGlow { get { return Mathf.Lerp(0.25f, 0.6f, Level); } }
    }
}
