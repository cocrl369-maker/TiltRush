using System.Collections.Generic;

namespace TiltRush
{
    /// <summary>
    /// Colours are unlocked by completed campaign levels (table below, same order as Config.Palette).
    /// Skins have their own conditions in Skins.cs.
    /// </summary>
    public static class Unlocks
    {
        public static readonly int[] ColorLevel = { 0, 0, 0, 0, 0, 0, 0, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };

        public static int Done { get { return SaveManager.Data.campaignDone; } }
        public static bool ColorUnlocked(int i) { return Done >= ColorLevel[i]; }

        /// <summary>Text like "Kite skin, Sky colour" for completing level number 'levelNumber' (1-based).</summary>
        public static string RewardsFor(int levelNumber)
        {
            var list = new List<string>();
            foreach (var s in Skins.All)
                if (s.Kind == UnlockKind.Campaign && s.Value == levelNumber) list.Add(s.Name + " skin");
            for (int i = 0; i < ColorLevel.Length; i++)
                if (ColorLevel[i] == levelNumber) list.Add(Config.PaletteNames[i] + " colour");
            return string.Join(", ", list.ToArray());
        }

        /// <summary>Only the colours unlocked by a campaign level (skins are announced by Skins.CollectNew).</summary>
        public static List<string> ColoursFor(int levelNumber)
        {
            var list = new List<string>();
            for (int i = 0; i < ColorLevel.Length; i++)
                if (ColorLevel[i] == levelNumber) list.Add(Config.PaletteNames[i] + " colour");
            return list;
        }
    }
}
