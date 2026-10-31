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
        public float bestDodge;
        public int bestDodgeScore;
        public string playerName = "";                    // shown on the leaderboard
        public bool screenShake = true;

        // Wardrobe
        public int playerShape = 0; // index into Config.PlayerShapes
        public int primary = 0;    // Config.Palette index
        public int outline = 7;     // White

        // Options
        public float musicVolume = 0.7f;
        public float sfxVolume = 0.8f;
        public float tiltSensitivity = 3f;

        // Tilt setup: 0 Auto (pose at round start), 1 Flat, 2 Relaxed, 3 Upright, 4 Lying down, 5 Custom (calibrated)
        public int tiltPreset = 0;
        public bool hasCustomTilt;
        public Vector3 customNeutral;
        public int customOrientation;

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

        public static PlayerStyle FromSave(int playerIndex = 0)
        {
            var d = SaveManager.Data;
            int n = Config.Palette.Length;
            int sh = Unlocks.ShapeUnlocked(d.playerShape % Config.PlayerShapes.Length) ? d.playerShape % Config.PlayerShapes.Length : 0;
            int pi = Unlocks.ColorUnlocked(d.primary % n) ? d.primary % n : 0;
            int oi = Unlocks.ColorUnlocked(d.outline % n) ? d.outline % n : 7;
            var s = new PlayerStyle { Shape = Config.PlayerShapes[sh], Primary = Config.Palette[pi], Outline = Config.Palette[oi] };
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
