using UnityEngine;

namespace TiltRush
{
    public enum GameModeId { Campaign, Classic, Hard, Dodge, Coop, Online }

    /// <summary>Pure data describing a mode. Add a new mode by adding a ModeDef to Modes.All.</summary>
    public class ModeDef
    {
        public GameModeId Id;
        public string Name;
        public string Description;
        public Color Color;
        public int Players = 1;
        public bool Gadgets = true;
        public bool LaunchRings = true;
        public float EnemySpeedMul = 1f;
        public float SpawnRateMul = 1f;
        public float StartDifficulty = 0f;
        public int BestOf = 0;            // 0 = single endless run
        public float HazardRateMul = 1f;  // how often red walls / swords come
        public float RampSeconds = 0f;    // time until full difficulty (0 = Config.DifficultyRampSeconds)
    }

    public static class Modes
    {
        public static readonly ModeDef Campaign = new ModeDef
        {
            Id = GameModeId.Campaign, Name = "CAMPAIGN", Color = Neon.Blue,
            Description = "12 levels with goals and obstacles. Unlock skins and colours.",
            LaunchRings = false   // each level decides for itself
        };

        public static readonly ModeDef Classic = new ModeDef
        {
            Id = GameModeId.Classic, Name = "CLASSIC", Color = Neon.Lime,
            Description = "Endless run. Score points, beat the leaderboard."
        };

        public static readonly ModeDef Hard = new ModeDef
        {
            Id = GameModeId.Hard, Name = "HARD", Color = Neon.Red,
            Description = "Like Classic - but with many more dots.",
            EnemySpeedMul = 1.05f, SpawnRateMul = 2.2f, HazardRateMul = 1.3f, StartDifficulty = 0.1f
        };

        public static readonly ModeDef Dodge = new ModeDef
        {
            Id = GameModeId.Dodge, Name = "DODGE", Color = Neon.Pink,
            Description = "No chasing: dodge shapes, waves and gags.",
            Gadgets = false, LaunchRings = false, RampSeconds = 110f
        };

        public static readonly ModeDef Coop = new ModeDef
        {
            Id = GameModeId.Coop, Name = "CO-OP DUEL", Color = Neon.Aqua,
            Description = "2 players, 1 map. Last one alive wins.",
            Players = 2, BestOf = Config.LocalBestOf
        };

        public static readonly ModeDef Online = new ModeDef
        {
            Id = GameModeId.Online, Name = "ONLINE PVP", Color = Neon.Pink,
            Description = "Best of 5. Same map, longest survival wins ELO.",
            BestOf = Config.OnlineBestOf
        };

        public static readonly ModeDef[] All = { Campaign, Classic, Hard, Dodge, Coop, Online };
    }
}
