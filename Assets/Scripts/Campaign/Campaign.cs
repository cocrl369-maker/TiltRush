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

        public int Dots { get { return Normal + Fast + Big; } }
    }

    /// <summary>The campaign. Add / change levels here - everything else adapts automatically.</summary>
    public static class Campaign
    {
        static ObstacleDef C(float x, float y, float r) { return new ObstacleDef { Circle = true, X = x, Y = y, A = r }; }
        static ObstacleDef R(float x, float y, float w, float h) { return new ObstacleDef { X = x, Y = y, A = w, B = h }; }

        public static readonly LevelDef[] Levels =
        {
            new LevelDef { Name = "First Steps",   Duration = 30, Normal = 18, DiffStart = 0.00f, DiffEnd = 0.10f },
            new LevelDef { Name = "Warm Up",       Duration = 40, Normal = 30, Fast = 4, DiffStart = 0.05f, DiffEnd = 0.20f },
            new LevelDef { Name = "Speed Bumps",   Duration = 45, Normal = 30, Fast = 14, DiffStart = 0.10f, DiffEnd = 0.30f },
            new LevelDef { Name = "Heavy Traffic", Duration = 50, Normal = 30, Fast = 12, Big = 6, DiffStart = 0.15f, DiffEnd = 0.35f },
            new LevelDef { Name = "Red Wall",      Duration = 50, Normal = 30, Fast = 12, Big = 6, Walls = 4, DiffStart = 0.20f, DiffEnd = 0.40f },
            new LevelDef { Name = "Pillars",       Duration = 55, Normal = 36, Fast = 14, Big = 6, Walls = 3, DiffStart = 0.20f, DiffEnd = 0.40f,
                           Obstacles = new[] { C(-0.25f, 0.15f, 0.9f), C(0.25f, -0.15f, 0.9f), C(0.0f, 0.32f, 0.7f) } },
            new LevelDef { Name = "Launch Pad",    Duration = 60, Normal = 40, Fast = 16, Big = 8, Walls = 4, Rings = true, DiffStart = 0.25f, DiffEnd = 0.45f,
                           Obstacles = new[] { C(-0.3f, -0.2f, 1.0f), C(0.3f, 0.2f, 1.0f), R(0.0f, 0.3f, 2.4f, 0.5f), R(0.0f, -0.3f, 2.4f, 0.5f) } },
            new LevelDef { Name = "Blades",        Duration = 60, Normal = 36, Fast = 14, Big = 6, Walls = 3, Swords = 5, DiffStart = 0.30f, DiffEnd = 0.55f,
                           Obstacles = new[] { C(-0.28f, 0.0f, 0.9f), C(0.28f, 0.0f, 0.9f), C(0.0f, 0.3f, 0.7f) } },
            new LevelDef { Name = "Corridors",     Duration = 65, Normal = 45, Fast = 18, Big = 8, Walls = 4, Swords = 4, Rings = true, DiffStart = 0.35f, DiffEnd = 0.60f,
                           Obstacles = new[] { R(-0.2f, 0.2f, 3.2f, 0.6f), R(0.2f, -0.2f, 3.2f, 0.6f), C(0.0f, 0.36f, 0.8f), C(0.0f, -0.36f, 0.8f) } },
            new LevelDef { Name = "Crossfire",     Duration = 70, Normal = 55, Fast = 24, Big = 10, Walls = 6, Swords = 6, Rings = true, DiffStart = 0.40f, DiffEnd = 0.70f,
                           Obstacles = new[] { C(-0.3f, 0.2f, 1.0f), C(0.3f, -0.2f, 1.0f), C(-0.3f, -0.28f, 0.8f), C(0.3f, 0.28f, 0.8f), R(0.0f, 0.0f, 0.6f, 2.4f) } },
            new LevelDef { Name = "Gauntlet",      Duration = 80, Normal = 70, Fast = 30, Big = 12, Walls = 7, Swords = 8, Rings = true, DiffStart = 0.50f, DiffEnd = 0.80f,
                           Obstacles = new[] { R(-0.25f, 0.2f, 3.4f, 0.6f), R(0.25f, -0.2f, 3.4f, 0.6f), C(-0.35f, -0.25f, 0.9f), C(0.35f, 0.25f, 0.9f), C(0.0f, 0.38f, 0.7f), C(0.0f, -0.38f, 0.7f) } },
            new LevelDef { Name = "Finale",        Duration = 90, Normal = 90, Fast = 40, Big = 16, Walls = 8, Swords = 10, Rings = true, DiffStart = 0.60f, DiffEnd = 0.90f,
                           Obstacles = new[] { C(-0.3f, 0.25f, 1.0f), C(0.3f, -0.25f, 1.0f), R(0.0f, 0.3f, 3.0f, 0.5f), R(0.0f, -0.3f, 3.0f, 0.5f), C(-0.38f, -0.2f, 0.7f), C(0.38f, 0.2f, 0.7f) } },
        };
    }
}
