using UnityEngine;

namespace TiltRush
{
    /// <summary>Neon colour set used everywhere (UI, wardrobe, effects).</summary>
    public static class Neon
    {
        public static readonly Color Lime   = new Color32(125, 255, 0, 255);
        public static readonly Color Green  = new Color32(0, 255, 100, 255);
        public static readonly Color Aqua   = new Color32(0, 255, 255, 255);
        public static readonly Color Blue   = new Color32(0, 120, 255, 255);
        public static readonly Color Indigo = new Color32(120, 60, 255, 255);
        public static readonly Color Purple = new Color32(200, 0, 255, 255);
        public static readonly Color Pink   = new Color32(255, 60, 200, 255);
        public static readonly Color Red    = new Color32(255, 40, 40, 255);
        public static readonly Color Orange = new Color32(255, 130, 0, 255);
        public static readonly Color Yellow = new Color32(255, 255, 0, 255);
        public static readonly Color White  = Color.white;
        public static readonly Color Gray   = new Color32(110, 110, 120, 255);
    }

    /// <summary>
    /// ALL global tuning values live here. Per-mode values are in GameModes.cs,
    /// gadget values in Gadgets.cs, enemy types in Enemy.cs / Hazards.cs.
    /// </summary>
    public static class Config
    {
        public const string GameTitle = "TILT RUSH";

        // Camera / arena. The arena always fills the whole screen (inside the safe area).
        // A BIGGER CameraSize = more world visible = everything looks smaller.
        public const float CameraSize = 5.8f;
        public const float ArenaMargin = 0.2f;
        public static Vector2 ArenaSize = new Vector2(20f, 11f);   // overwritten automatically by App.FitArena()
        public static readonly Vector2 OnlineArenaSize = new Vector2(19f, 10.4f);   // online PvP: same size on every phone

        // Simulation safety: a long frame (hiccup, app resumed) is split into small sub-steps so nothing can tunnel
        // through walls / swords. Frames longer than MaxFrameTime are capped (the game slows down for a moment instead).
        public const float MaxSubStep = 1f / 50f;
        public const float MaxFrameTime = 0.1f;

        // Player
        public const float PlayerRadius = 0.26f;         // hit circle
        public const float PlayerVisualRadius = 0.4f;    // how big the ship is drawn (the tips are forgiving)
        public const float PlayerMaxSpeed = 9f;          // was 8 - a little faster
        public const float PlayerAcceleration = 47f;

        public const float RespawnInvulnerable = 2.5f;   // seconds of blinking invulnerability after losing a life

        // Dots (normal enemies)
        public const int MaxEnemies = 260;
        public const float EnemySpeedGrowth = 1.4f;       // dots get (1 + difficulty * this) times faster
        public const float EnemySpawnWarning = 0.8f;      // seconds a new dot is harmless
        public const float SpawnIntervalStart = 1.6f;     // seconds between spawns at difficulty 0
        public const float SpawnIntervalEnd = 0.14f;      // ... at difficulty 1
        public const float DifficultyRampSeconds = 150f;  // time until difficulty = 1

        // Red walls (appear when difficulty gets higher)
        public const float WallStartDifficulty = 0.3f;
        public const float WallIntervalStart = 12f;
        public const float WallIntervalEnd = 5f;
        public const float WallSpeed = 1.9f;
        public const float WallThickness = 0.3f;
        public const int MaxWalls = 3;

        // Swords (appear last, announced by a light red lane)
        public const float SwordStartDifficulty = 0.55f;
        public const float SwordIntervalStart = 9f;
        public const float SwordIntervalEnd = 3.2f;
        public const float SwordSpeed = 19f;
        public const float SwordWarning = 1.4f;           // seconds the lane is shown before the sword flies
        public const float SwordLength = 3.2f;
        public const float SwordLaneWidth = 0.9f;

        // Score: points per second survived, growing with difficulty
        public const float ScorePerSecond = 10f;
        public const float ScoreDifficultyBonus = 2f;     // at max difficulty you earn (1 + 2) = 3x per second

        // Bonus points (also multiplied by the difficulty factor above)
        public static readonly int[] KillPoints = { 10, 20, 40, 50, 25, 12, 8 };   // Normal, Fast, Big, Asteroid, Rock, Pebble, Shard

        // Asteroid field
        public const float CannonInterval = 0.15f;        // seconds between shots
        public const float BulletSpeed = 17f;
        public const float BulletLife = 1.3f;
        public const int GadgetPoints = 50;                         // for every pickup collected
        public const float NukeKillMultiplier = 0.25f;              // nuke kills count less so it can't dominate the score

        // Gadgets
        public const float GadgetSpawnInterval = 2.5f;   // average seconds between pickups
        public const float GadgetLifetime = 8f;
        public const float SlowMoScale = 0.35f;
        public const float ShockwaveRadius = 6.5f;
        public const float FreezeRadius = 8f;        // a bit bigger than the shockwave
        public const float FreezeDuration = 4.5f;    // frozen dots can be smashed by touching them

        // Launch rings (yellow orbs)
        public const float LaunchRingStartTime = 60f;     // rings only appear after this many seconds
        public const int LaunchRingsPerPlayer = 2;
        public const float LaunchRingLife = 14f;
        public const float LaunchForce = 20f;
        public const float LaunchDamping = 3.5f;

        // Online (Unity Gaming Services) - the leaderboard must exist in the Unity Cloud dashboard with this ID
        public const float MatchmakingSeconds = 25f;      // then you play against a bot

        // ---- Ads (Unity Ads). Fill in the Game ID from Unity Cloud > Monetization, and set AdsTestMode = false
        //      for the version you upload to the store (test ads earn nothing, real ads in testing break the rules).
        public const string AdsGameIdAndroid = "800420854";      // Unity Cloud > Monetization > Apps
        public const string AdsGameIdIos = "";
        public const string AdsInterstitialAndroid = "BP_Interstitial_Android";   // placement ID from the dashboard
        public const string AdsInterstitialIos = "Interstitial_iOS";
        public const bool AdsTestMode = true;
        public const float AdIntervalSeconds = 120f;             // at most one ad every 2 minutes (never during a round)
        public const string RemoveAdsProductId = "remove_ads";   // in-app product (Google Play Console)

        // ---- Store / legal: web address of your privacy policy (shown in Options > Profile)
        public const string PrivacyPolicyUrl = "https://sites.google.com/view/tiltrush/startseite/datenschutz";

        // Elo & matches
        public const int StartElo = 400;
        public const int EloK = 32;
        public const int OnlineBestOf = 5;
        public const int LocalBestOf = 1;   // set to 3 or 5 for a local series

        // Player skins and their unlock conditions: Core/Skins.cs

        // Colours: the first 8 are free, the other 12 are unlocked through the campaign
        public static readonly Color[] Palette =
        {
            new Color32(125, 255, 0, 255),   // Lime
            new Color32(0, 255, 255, 255),   // Aqua
            new Color32(0, 120, 255, 255),   // Blue
            new Color32(255, 60, 200, 255),  // Pink
            new Color32(255, 40, 40, 255),   // Red
            new Color32(255, 130, 0, 255),   // Orange
            new Color32(255, 255, 0, 255),   // Yellow
            new Color32(255, 255, 255, 255), // White
            new Color32(120, 255, 200, 255), // Mint
            new Color32(110, 190, 255, 255), // Sky
            new Color32(190, 160, 255, 255), // Lavender
            new Color32(255, 120, 110, 255), // Coral
            new Color32(255, 200, 60, 255),  // Gold
            new Color32(0, 190, 170, 255),   // Teal
            new Color32(255, 120, 170, 255), // Rose
            new Color32(150, 60, 255, 255),  // Violet
            new Color32(200, 20, 60, 255),   // Crimson
            new Color32(200, 205, 215, 255), // Silver
            new Color32(110, 130, 160, 255), // Steel
            new Color32(255, 90, 20, 255)    // Ember
        };
        public static readonly string[] PaletteNames =
        {
            "Lime", "Aqua", "Blue", "Pink", "Red", "Orange", "Yellow", "White",
            "Mint", "Sky", "Lavender", "Coral", "Gold", "Teal", "Rose", "Violet", "Crimson", "Silver", "Steel", "Ember"
        };
    }
}
