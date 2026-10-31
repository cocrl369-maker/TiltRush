namespace TiltRush
{
    /// <summary>Permanent, non-lethal obstacle. Positions are relative to the arena (-0.5..0.5), sizes in world units.</summary>
    public class ObstacleDef
    {
        public bool Circle;
        public float X, Y;     // centre, -0.5..0.5 of the arena width / height
        public float A, B;     // circle: A = radius; rectangle: A = width, B = height
    }

    /// <summary>One campaign level: how long, which enemies (and how many), which extras, which obstacles.</summary>
    public class LevelDef
    {
        public string Name;
        public float Duration;                         // seconds you have to survive
        public int Normal, Fast, Big;                  // number of dots, spread evenly over the level
        public int Walls, Swords;                      // number of red walls / swords
        public bool Gadgets = true;                    // pickups on the map
        public bool Rings;                             // yellow launch rings
        public float DiffStart, DiffEnd;               // 0..1, controls the speed of the enemies during the level
        public ObstacleDef[] Obstacles = new ObstacleDef[0];
        public LevelTheme Theme = LevelThemes.Neon;    // colours, background, particles, block style

        public int Dots { get { return Normal + Fast + Big; } }
    }

    /// <summary>The campaign. Add / change levels here - everything else adapts automatically.</summary>
    public static class Campaign
    {
        static ObstacleDef C(float x, float y, float r) { return new ObstacleDef { Circle = true, X = x, Y = y, A = r }; }
        static ObstacleDef R(float x, float y, float w, float h) { return new ObstacleDef { X = x, Y = y, A = w, B = h }; }

        // shortcuts for the themes
        static readonly LevelTheme NEON = LevelThemes.Neon, ICE = LevelThemes.Ice, LAVA = LevelThemes.Lava, TOXIC = LevelThemes.Toxic,
            DESERT = LevelThemes.Desert, SEA = LevelThemes.DeepSea, SPACE = LevelThemes.Space, CANDY = LevelThemes.Candy,
            STORM = LevelThemes.Storm, SUNSET = LevelThemes.Sunset, CYBER = LevelThemes.Cyber, CHAOS = LevelThemes.Chaos;

        public static readonly LevelDef[] Levels =
        {
            // ---- 1-6: learning
            new LevelDef { Name = "First Steps",   Theme = NEON,   Duration = 30, Normal = 18, DiffStart = 0.00f, DiffEnd = 0.10f },
            new LevelDef { Name = "Warm Up",       Theme = SUNSET, Duration = 40, Normal = 30, Fast = 4, DiffStart = 0.05f, DiffEnd = 0.20f },
            new LevelDef { Name = "Speed Bumps",   Theme = DESERT, Duration = 45, Normal = 30, Fast = 14, DiffStart = 0.10f, DiffEnd = 0.30f,
                           Obstacles = new[] { R(-0.25f, 0f, 1.6f, 0.6f), R(0.25f, 0f, 1.6f, 0.6f) } },
            new LevelDef { Name = "Heavy Traffic", Theme = SEA,    Duration = 50, Normal = 30, Fast = 12, Big = 6, DiffStart = 0.15f, DiffEnd = 0.35f },
            new LevelDef { Name = "Red Wall",      Theme = LAVA,   Duration = 50, Normal = 30, Fast = 12, Big = 6, Walls = 4, DiffStart = 0.20f, DiffEnd = 0.40f,
                           Obstacles = new[] { R(0f, 0.3f, 2.6f, 0.7f), R(0f, -0.3f, 2.6f, 0.7f) } },
            new LevelDef { Name = "Pillars",       Theme = ICE,    Duration = 55, Normal = 36, Fast = 14, Big = 6, Walls = 3, DiffStart = 0.20f, DiffEnd = 0.40f,
                           Obstacles = new[] { C(-0.25f, 0.15f, 0.9f), C(0.25f, -0.15f, 0.9f), C(0.0f, 0.32f, 0.7f) } },
            // ---- 7-12: everything comes together
            new LevelDef { Name = "Launch Pad",    Theme = SUNSET, Duration = 60, Normal = 40, Fast = 16, Big = 8, Walls = 4, Rings = true, DiffStart = 0.25f, DiffEnd = 0.45f,
                           Obstacles = new[] { C(-0.3f, -0.2f, 1.0f), C(0.3f, 0.2f, 1.0f), R(0.0f, 0.3f, 2.4f, 0.5f), R(0.0f, -0.3f, 2.4f, 0.5f) } },
            new LevelDef { Name = "Blades",        Theme = STORM,  Duration = 60, Normal = 36, Fast = 14, Big = 6, Walls = 3, Swords = 5, DiffStart = 0.30f, DiffEnd = 0.55f,
                           Obstacles = new[] { C(-0.28f, 0.0f, 0.9f), C(0.28f, 0.0f, 0.9f), C(0.0f, 0.3f, 0.7f) } },
            new LevelDef { Name = "Corridors",     Theme = TOXIC,  Duration = 65, Normal = 45, Fast = 18, Big = 8, Walls = 4, Swords = 4, Rings = true, DiffStart = 0.35f, DiffEnd = 0.60f,
                           Obstacles = new[] { R(-0.2f, 0.2f, 3.2f, 0.6f), R(0.2f, -0.2f, 3.2f, 0.6f), C(0.0f, 0.36f, 0.8f), C(0.0f, -0.36f, 0.8f) } },
            new LevelDef { Name = "Crossfire",     Theme = SEA,    Duration = 70, Normal = 55, Fast = 24, Big = 10, Walls = 6, Swords = 6, Rings = true, DiffStart = 0.40f, DiffEnd = 0.70f,
                           Obstacles = new[] { C(-0.3f, 0.2f, 1.0f), C(0.3f, -0.2f, 1.0f), C(-0.3f, -0.28f, 0.8f), C(0.3f, 0.28f, 0.8f), R(0.0f, 0.0f, 0.6f, 2.4f) } },
            new LevelDef { Name = "Gauntlet",      Theme = SPACE,  Duration = 80, Normal = 70, Fast = 30, Big = 12, Walls = 7, Swords = 8, Rings = true, DiffStart = 0.50f, DiffEnd = 0.80f,
                           Obstacles = new[] { R(-0.25f, 0.2f, 3.4f, 0.6f), R(0.25f, -0.2f, 3.4f, 0.6f), C(-0.35f, -0.25f, 0.9f), C(0.35f, 0.25f, 0.9f), C(0.0f, 0.38f, 0.7f), C(0.0f, -0.38f, 0.7f) } },
            new LevelDef { Name = "Lava Core",     Theme = LAVA,   Duration = 85, Normal = 80, Fast = 34, Big = 14, Walls = 8, Swords = 9, Rings = true, DiffStart = 0.55f, DiffEnd = 0.85f,
                           Obstacles = new[] { C(-0.3f, 0.25f, 1.0f), C(0.3f, -0.25f, 1.0f), R(0.0f, 0.3f, 3.0f, 0.6f), R(0.0f, -0.3f, 3.0f, 0.6f), C(-0.38f, -0.2f, 0.7f), C(0.38f, 0.2f, 0.7f) } },
            // ---- 13-19: new worlds
            new LevelDef { Name = "Candy Rush",    Theme = CANDY,  Duration = 75, Normal = 70, Fast = 34, Big = 8, Walls = 5, Swords = 6, Rings = true, DiffStart = 0.45f, DiffEnd = 0.75f,
                           Obstacles = new[] { R(-0.33f, 0f, 0.6f, 3.2f), R(0f, 0.2f, 0.6f, 2.6f), R(0.33f, 0f, 0.6f, 3.2f) } },
            new LevelDef { Name = "Frozen Lake",   Theme = ICE,    Duration = 80, Normal = 60, Fast = 50, Big = 8, Walls = 6, Swords = 6, DiffStart = 0.50f, DiffEnd = 0.78f,
                           Obstacles = new[] { C(-0.35f, 0.25f, 0.6f), C(-0.1f, -0.2f, 0.7f), C(0.15f, 0.3f, 0.6f), C(0.38f, -0.15f, 0.8f), C(0f, 0.05f, 0.5f) } },
            new LevelDef { Name = "Sandstorm",     Theme = DESERT, Duration = 80, Normal = 75, Fast = 30, Big = 12, Walls = 10, Swords = 6, Rings = true, DiffStart = 0.50f, DiffEnd = 0.80f,
                           Obstacles = new[] { R(-0.3f, 0.28f, 2.2f, 0.9f), R(0.3f, -0.28f, 2.2f, 0.9f), R(0f, 0f, 1.2f, 1.2f) } },
            new LevelDef { Name = "Abyss",         Theme = SEA,    Duration = 85, Normal = 70, Fast = 24, Big = 30, Walls = 6, Swords = 8, Rings = true, DiffStart = 0.55f, DiffEnd = 0.82f,
                           Obstacles = new[] { C(-0.3f, 0f, 1.2f), C(0.3f, 0f, 1.2f), C(0f, 0.36f, 0.6f), C(0f, -0.36f, 0.6f) } },
            new LevelDef { Name = "Neon Maze",     Theme = CYBER,  Duration = 85, Normal = 80, Fast = 34, Big = 12, Walls = 6, Swords = 10, Rings = true, DiffStart = 0.55f, DiffEnd = 0.85f,
                           Obstacles = new[] { R(-0.3f, 0.22f, 0.5f, 2.4f), R(-0.1f, -0.22f, 0.5f, 2.4f), R(0.1f, 0.22f, 0.5f, 2.4f), R(0.3f, -0.22f, 0.5f, 2.4f) } },
            new LevelDef { Name = "Meteor Shower", Theme = SPACE,  Duration = 90, Normal = 75, Fast = 36, Big = 14, Walls = 5, Swords = 16, Rings = true, DiffStart = 0.60f, DiffEnd = 0.85f,
                           Obstacles = new[] { C(-0.32f, 0.3f, 0.7f), C(0.32f, 0.3f, 0.7f), C(-0.32f, -0.3f, 0.7f), C(0.32f, -0.3f, 0.7f) } },
            new LevelDef { Name = "Acid Rain",     Theme = TOXIC,  Duration = 90, Normal = 90, Fast = 40, Big = 14, Walls = 9, Swords = 9, Rings = true, DiffStart = 0.60f, DiffEnd = 0.88f,
                           Obstacles = new[] { R(0f, 0.3f, 4.0f, 0.5f), C(-0.3f, -0.15f, 0.8f), C(0.3f, -0.15f, 0.8f) } },
            // ---- 20-25: the hard part
            new LevelDef { Name = "Thunderdome",   Theme = STORM,  Duration = 95, Normal = 95, Fast = 42, Big = 16, Walls = 8, Swords = 14, Rings = true, DiffStart = 0.65f, DiffEnd = 0.90f,
                           Obstacles = new[] { C(0f, 0.35f, 0.6f), C(0.3f, 0.18f, 0.6f), C(0.3f, -0.18f, 0.6f), C(0f, -0.35f, 0.6f), C(-0.3f, -0.18f, 0.6f), C(-0.3f, 0.18f, 0.6f) } },
            new LevelDef { Name = "Magma Chamber", Theme = LAVA,   Duration = 100, Normal = 100, Fast = 44, Big = 20, Walls = 10, Swords = 12, Rings = true, DiffStart = 0.68f, DiffEnd = 0.92f,
                           Obstacles = new[] { R(-0.28f, 0.25f, 2.0f, 0.7f), R(0.28f, 0.25f, 2.0f, 0.7f), R(0f, -0.28f, 3.4f, 0.7f), C(0f, 0.1f, 0.7f) } },
            new LevelDef { Name = "Sugar Crash",   Theme = CANDY,  Duration = 100, Normal = 95, Fast = 60, Big = 12, Walls = 9, Swords = 14, Rings = true, DiffStart = 0.70f, DiffEnd = 0.93f,
                           Obstacles = new[] { C(-0.25f, 0.25f, 0.8f), C(0.25f, -0.25f, 0.8f), R(-0.25f, -0.25f, 1.6f, 0.6f), R(0.25f, 0.25f, 1.6f, 0.6f) } },
            new LevelDef { Name = "Glacier",       Theme = ICE,    Duration = 110, Normal = 105, Fast = 55, Big = 22, Walls = 10, Swords = 15, Rings = true, DiffStart = 0.72f, DiffEnd = 0.95f,
                           Obstacles = new[] { R(-0.3f, 0f, 0.7f, 3.0f), R(0.3f, 0f, 0.7f, 3.0f), C(0f, 0.32f, 0.8f), C(0f, -0.32f, 0.8f) } },
            new LevelDef { Name = "Overdrive",     Theme = CYBER,  Duration = 115, Normal = 115, Fast = 65, Big = 22, Walls = 11, Swords = 18, Rings = true, DiffStart = 0.75f, DiffEnd = 0.97f,
                           Obstacles = new[] { R(-0.22f, 0.3f, 2.2f, 0.5f), R(0.22f, -0.3f, 2.2f, 0.5f), R(-0.35f, -0.1f, 0.5f, 1.8f), R(0.35f, 0.1f, 0.5f, 1.8f) } },
            new LevelDef { Name = "Chaos Theory",  Theme = CHAOS,  Duration = 120, Normal = 125, Fast = 70, Big = 26, Walls = 12, Swords = 22, Rings = true, DiffStart = 0.80f, DiffEnd = 1.00f,
                           Obstacles = new[] { C(-0.3f, 0.25f, 0.8f), C(0.3f, -0.25f, 0.8f), C(0.3f, 0.25f, 0.6f), C(-0.3f, -0.25f, 0.6f), R(0f, 0f, 1.4f, 0.5f) } },
        };
    }
}
