using System;
using UnityEngine;

namespace TiltRush
{
    [Serializable]
    public class SaveData
    {
        public int elo = Config.StartElo;
        public int onlineWins, onlineLosses;
        public float bestClassic, bestHard;               // best survival time (seconds)
        public int bestClassicScore, bestHardScore;       // best points
        public float bestDodge, bestAsteroids;
        public int bestAsteroidsScore;
        public int bestDodgeScore;
        public string playerName = "";                    // shown on the leaderboard

        // Statistics (skins are unlocked with these, see Skins.cs)
        public int totalKills, totalPickups, runsPlayed;
        public float totalPlaySeconds;
        public long knownSkins;                           // bit mask: skins already announced as unlocked
        public bool skinsInitialized;

        // Wardrobe
        public int playerShape = 20; // index into Skins.All (20 = STARFIGHTER)
        public int primary = 0;    // Config.Palette index
        public int outline = 7;     // White

        // Options - GRAPHICS
        public int quality = 2;            // 0 Low, 1 Medium, 2 High, 3 Custom
        public int particles = 2;          // 0 Off, 1 Few, 2 Many
        public bool trail = true;          // light trail behind the ship
        public int bgMotion = 2;           // 0 Still, 1 Calm, 2 Full
        public float bgBrightness = 1f;    // 0.5 .. 1.2
        public bool screenShake = true;
        public int frameRate = 1;          // 0 = 30, 1 = 60, 2 = 120 fps

        // Options - SOUND
        public float musicVolume = 0.7f;
        public float sfxVolume = 0.8f;
        public bool buttonSounds = true;
        public bool vibration = true;

        // Options - INTERFACE
        public int hudSize = 1;            // 0 Small, 1 Normal, 2 Large
        public bool pauseLeft;             // pause button on the left side
        public bool showFps;
        public bool menuAnimations = true;

        // Options - CONTROLS
        public float tiltSensitivity = 3f;
        public int tiltSmoothing = 1;      // 0 Direct, 1 Normal, 2 Smooth
        public int tiltAxes;               // 0 Standard (screen axes), 1 Swapped (raw device axes, for unusual phones)
        public bool invertX, invertY;

        // Tilt setup: 0 Auto (pose at round start), 1 Flat, 2 Relaxed, 3 Upright, 4 Lying down, 5 Custom (calibrated)
        public int tiltPreset = 0;
        public bool hasCustomTilt;
        public Vector3 customNeutral;
        public int customOrientation;

        public bool tutorialDone;          // the tutorial runs automatically on the very first start

        // Campaign: number of levels completed (in order)
        public int campaignDone = 0;
    }

    public static class SaveManager
    {
        const string Key = "tiltrush_save";
        public static SaveData Data { get; private set; }

        public static void Load()
        {
            if (PlayerPrefs.HasKey(Key))
                Data = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(Key));
            if (Data == null) Data = new SaveData();
            if (string.IsNullOrEmpty(Data.playerName)) { Data.playerName = "Player" + UnityEngine.Random.Range(1000, 9999); Save(); }
            if (!Data.skinsInitialized) { Data.skinsInitialized = true; Skins.MarkAllKnown(); Save(); }
        }

        /// <summary>Deletes progress, scores and statistics, but keeps the settings and the player name.</summary>
        public static void ResetProgress()
        {
            var old = Data;
            var fresh = new SaveData();
            // copy all settings
            fresh.playerName = old.playerName;
            fresh.quality = old.quality; fresh.particles = old.particles; fresh.trail = old.trail; fresh.bgMotion = old.bgMotion;
            fresh.bgBrightness = old.bgBrightness; fresh.screenShake = old.screenShake; fresh.frameRate = old.frameRate;
            fresh.musicVolume = old.musicVolume; fresh.sfxVolume = old.sfxVolume; fresh.buttonSounds = old.buttonSounds; fresh.vibration = old.vibration;
            fresh.hudSize = old.hudSize; fresh.pauseLeft = old.pauseLeft; fresh.showFps = old.showFps; fresh.menuAnimations = old.menuAnimations;
            fresh.tiltSensitivity = old.tiltSensitivity; fresh.tiltSmoothing = old.tiltSmoothing; fresh.invertX = old.invertX; fresh.invertY = old.invertY; fresh.tiltAxes = old.tiltAxes;
            fresh.tiltPreset = old.tiltPreset; fresh.hasCustomTilt = old.hasCustomTilt; fresh.customNeutral = old.customNeutral; fresh.customOrientation = old.customOrientation;
            Data = fresh;
            Data.skinsInitialized = true;
            Skins.MarkAllKnown();
            Save();
        }

        public static void Save()
        {
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(Data));
            PlayerPrefs.Save();
        }
    }

    public static class EloCalculator
    {
        /// <summary>Elo change for 'my' player (positive when won).</summary>
        public static int Delta(int my, int opp, bool won)
        {
            double expected = 1.0 / (1.0 + Math.Pow(10.0, (opp - my) / 400.0));
            return (int)Math.Round(Config.EloK * ((won ? 1.0 : 0.0) - expected));
        }
    }

    public struct PlayerStyle
    {
        public ShapeType Shape;
        public Color Primary, Outline;
        public Sprite Detail;          // second-colour detail of the skin (drawn in the outline colour), may be null

        public static PlayerStyle FromSave(int playerIndex = 0)
        {
            var d = SaveManager.Data;
            int n = Config.Palette.Length;
            int sh = Mathf.Abs(d.playerShape) % Skins.All.Length;
            if (!Skins.All[sh].Unlocked) sh = 0;
            var skin = Skins.All[sh];
            int pi = Unlocks.ColorUnlocked(d.primary % n) ? d.primary % n : 0;
            int oi = Unlocks.ColorUnlocked(d.outline % n) ? d.outline % n : 7;
            var s = new PlayerStyle { Shape = skin.Shape, Primary = Config.Palette[pi], Outline = Config.Palette[oi],
                                      Detail = SpriteFactory.Detail(skin.Shape, skin.Detail) };
            if (playerIndex == 1)                       // player 2 gets a different (unlocked) colour
            {
                int p2 = pi;
                for (int k = 5; k < 5 + n; k++)
                {
                    int c = (pi + k) % n;
                    if (c != pi && c != oi && Unlocks.ColorUnlocked(c)) { p2 = c; break; }
                }
                s.Primary = Config.Palette[p2];
            }
            return s;
        }
    }
}
