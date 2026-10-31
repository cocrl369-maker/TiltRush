using System.Collections.Generic;

namespace TiltRush
{
    /// <summary>
    /// Which skins / colours are unlocked: an item is available when the player has completed at least
    /// N campaign levels. Edit the two tables to change the rewards (same order as Config.PlayerShapes / Config.Palette).
    /// </summary>
    public static class Unlocks
    {
        public static readonly int[] ShapeLevel = { 0, 0, 2, 4, 6, 8, 10, 12 };
        public static readonly int[] ColorLevel = { 0, 0, 0, 0, 0, 0, 0, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };

        public static int Done { get { return SaveManager.Data.campaignDone; } }
        public static bool ShapeUnlocked(int i) { return Done >= ShapeLevel[i]; }
        public static bool ColorUnlocked(int i) { return Done >= ColorLevel[i]; }

        /// <summary>Text like "Kite skin, Sky colour" for completing level number 'levelNumber' (1-based).</summary>
        public static string RewardsFor(int levelNumber)
        {
            var list = new List<string>();
            for (int i = 0; i < ShapeLevel.Length; i++)
                if (ShapeLevel[i] == levelNumber) list.Add(Config.PlayerShapeNames[i].Substring(0, 1) + Config.PlayerShapeNames[i].Substring(1).ToLower() + " skin");
            for (int i = 0; i < ColorLevel.Length; i++)
                if (ColorLevel[i] == levelNumber) list.Add(Config.PaletteNames[i] + " colour");
            return string.Join(", ", list.ToArray());
        }
    }
}
