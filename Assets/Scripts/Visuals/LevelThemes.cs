using UnityEngine;

namespace TiltRush
{
    /// <summary>Shapes of the drifting background blocks.</summary>
    public enum BgPattern { Tiles, Bricks, Crystals, Bubbles, Stars }

    /// <summary>Small particles floating in the background (snow, embers ...).</summary>
    public enum Ambient { None, Sparkles, Snow, Embers, Bubbles, Stars, Rain, Dust }

    /// <summary>Pattern drawn on the obstacle blocks.</summary>
    public enum BlockPattern { Frame, Bricks, Shine, Dots, Rivets, Stripes, Lines }

    /// <summary>Look of the obstacle blocks (campaign).</summary>
    public class BlockStyle
    {
        public Color Fill, Edge, Detail;
        public float Glow = 0.2f;              // glow around the block (lava glows a lot)
        public BlockPattern Pattern;
    }

    /// <summary>
    /// A campaign theme: colours, background pattern, floating particles and the style of the obstacle blocks.
    /// The arena border uses Accent. Within a level the colours still get a bit more intense as time goes on.
    /// </summary>
    public class LevelTheme
    {
        public string Name;
        public Color Top, Bottom, Accent, Tile, Dark;
        public BgPattern Pattern;
        public Ambient Ambient;
        public BlockStyle Block;
        public bool LightRims;                 // enemy rims lighter (for red/orange backgrounds)
        public bool Cycle;                     // colours travel through the rainbow (final level)
    }

    /// <summary>All themes. Add a theme: one entry here, then use it in Campaign.cs.</summary>
    public static class LevelThemes
    {
        static Color C(byte r, byte g, byte b) { return new Color32(r, g, b, 255); }
        static Color C(byte r, byte g, byte b, byte a) { return new Color32(r, g, b, a); }

        public static readonly LevelTheme Neon = new LevelTheme
        {
            Name = "Neon City", Top = C(70, 120, 235), Bottom = C(18, 28, 90), Accent = C(140, 220, 255), Tile = C(170, 205, 255), Dark = C(8, 10, 30),
            Pattern = BgPattern.Tiles, Ambient = Ambient.Sparkles,
            Block = new BlockStyle { Fill = C(14, 18, 50), Edge = C(140, 220, 255), Detail = C(140, 220, 255, 110), Pattern = BlockPattern.Frame }
        };

        public static readonly LevelTheme Ice = new LevelTheme
        {
            Name = "Ice Cave", Top = C(175, 232, 255), Bottom = C(55, 135, 200), Accent = C(240, 252, 255), Tile = C(255, 255, 255), Dark = C(10, 40, 75),
            Pattern = BgPattern.Crystals, Ambient = Ambient.Snow,
            Block = new BlockStyle { Fill = C(150, 215, 245, 230), Edge = C(245, 255, 255), Detail = C(255, 255, 255, 200), Glow = 0.3f, Pattern = BlockPattern.Shine }
        };

        public static readonly LevelTheme Lava = new LevelTheme
        {
            Name = "Lava Lake", Top = C(140, 15, 60), Bottom = C(255, 105, 20), Accent = C(255, 195, 70), Tile = C(255, 150, 80), Dark = C(40, 4, 10),
            Pattern = BgPattern.Bricks, Ambient = Ambient.Embers, LightRims = true,
            Block = new BlockStyle { Fill = C(95, 14, 12), Edge = C(255, 165, 40), Detail = C(170, 40, 15), Glow = 0.55f, Pattern = BlockPattern.Bricks }
        };

        public static readonly LevelTheme Toxic = new LevelTheme
        {
            Name = "Toxic Swamp", Top = C(55, 140, 45), Bottom = C(10, 45, 25), Accent = C(180, 255, 60), Tile = C(200, 255, 150), Dark = C(5, 20, 8),
            Pattern = BgPattern.Bubbles, Ambient = Ambient.Bubbles,
            Block = new BlockStyle { Fill = C(35, 95, 25), Edge = C(180, 255, 60), Detail = C(120, 220, 50, 200), Glow = 0.35f, Pattern = BlockPattern.Dots }
        };

        public static readonly LevelTheme Desert = new LevelTheme
        {
            Name = "Desert Ruins", Top = C(255, 205, 115), Bottom = C(195, 105, 40), Accent = C(255, 245, 190), Tile = C(255, 235, 175), Dark = C(55, 25, 5),
            Pattern = BgPattern.Bricks, Ambient = Ambient.Dust,
            Block = new BlockStyle { Fill = C(210, 155, 85), Edge = C(110, 60, 25), Detail = C(160, 105, 50), Glow = 0.1f, Pattern = BlockPattern.Lines }
        };

        public static readonly LevelTheme DeepSea = new LevelTheme
        {
            Name = "Deep Sea", Top = C(20, 115, 175), Bottom = C(4, 18, 60), Accent = C(90, 235, 255), Tile = C(120, 220, 255), Dark = C(2, 10, 30),
            Pattern = BgPattern.Bubbles, Ambient = Ambient.Bubbles,
            Block = new BlockStyle { Fill = C(15, 60, 90), Edge = C(255, 120, 150), Detail = C(255, 150, 170, 170), Glow = 0.3f, Pattern = BlockPattern.Dots }
        };

        public static readonly LevelTheme Space = new LevelTheme
        {
            Name = "Outer Space", Top = C(70, 25, 120), Bottom = C(5, 4, 25), Accent = C(205, 165, 255), Tile = C(205, 185, 255), Dark = C(5, 3, 15),
            Pattern = BgPattern.Stars, Ambient = Ambient.Stars,
            Block = new BlockStyle { Fill = C(55, 60, 85), Edge = C(190, 200, 230), Detail = C(120, 130, 160), Glow = 0.15f, Pattern = BlockPattern.Rivets }
        };

        public static readonly LevelTheme Candy = new LevelTheme
        {
            Name = "Candy Land", Top = C(255, 160, 215), Bottom = C(170, 65, 165), Accent = C(255, 245, 255), Tile = C(255, 225, 245), Dark = C(60, 10, 50),
            Pattern = BgPattern.Bubbles, Ambient = Ambient.Sparkles,
            Block = new BlockStyle { Fill = C(255, 90, 160), Edge = C(255, 255, 255), Detail = C(255, 235, 245), Glow = 0.25f, Pattern = BlockPattern.Stripes }
        };

        public static readonly LevelTheme Storm = new LevelTheme
        {
            Name = "Thunder Storm", Top = C(95, 95, 150), Bottom = C(25, 25, 55), Accent = C(225, 225, 255), Tile = C(205, 205, 245), Dark = C(10, 10, 25),
            Pattern = BgPattern.Tiles, Ambient = Ambient.Rain,
            Block = new BlockStyle { Fill = C(45, 48, 70), Edge = C(250, 230, 120), Detail = C(150, 150, 190), Glow = 0.2f, Pattern = BlockPattern.Rivets }
        };

        public static readonly LevelTheme Sunset = new LevelTheme
        {
            Name = "Sunset Boulevard", Top = C(110, 40, 145), Bottom = C(255, 140, 60), Accent = C(255, 225, 125), Tile = C(255, 205, 165), Dark = C(40, 10, 30), LightRims = true,
            Pattern = BgPattern.Tiles, Ambient = Ambient.Sparkles,
            Block = new BlockStyle { Fill = C(60, 20, 60), Edge = C(255, 200, 110), Detail = C(255, 140, 90, 160), Glow = 0.3f, Pattern = BlockPattern.Frame }
        };

        public static readonly LevelTheme Cyber = new LevelTheme
        {
            Name = "Cyber Grid", Top = C(0, 170, 165), Bottom = C(45, 0, 85), Accent = C(0, 255, 230), Tile = C(130, 255, 240), Dark = C(5, 0, 20),
            Pattern = BgPattern.Tiles, Ambient = Ambient.Rain,
            Block = new BlockStyle { Fill = C(15, 5, 40), Edge = C(255, 40, 220), Detail = C(0, 255, 230, 150), Glow = 0.45f, Pattern = BlockPattern.Frame }
        };

        public static readonly LevelTheme Chaos = new LevelTheme
        {
            Name = "Chaos Dimension", Top = C(255, 60, 120), Bottom = C(40, 0, 90), Accent = C(255, 255, 255), Tile = C(255, 200, 255), Dark = C(10, 0, 20), Cycle = true,
            Pattern = BgPattern.Stars, Ambient = Ambient.Sparkles,
            Block = new BlockStyle { Fill = C(20, 5, 30), Edge = C(255, 255, 255), Detail = C(255, 120, 255, 170), Glow = 0.5f, Pattern = BlockPattern.Frame }
        };
    }
}
