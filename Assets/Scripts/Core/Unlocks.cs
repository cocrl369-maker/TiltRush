using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

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
        public static bool ColorUnlocked(int i) { return i >= 0 && i < ColorLevel.Length && Done >= ColorLevel[i]; }

        // ------------------------------------------------------------------ GRADIENT colour (changes its colour all the time)
        // It comes after the normal palette (index = Config.Palette.Length) and has its own conditions:
        //   body (primary) colour:  play more than PlayMinutesPerDay minutes on DaysNeeded different days
        //   outline colour:         open the game on DaysNeeded different days
        public const int DaysNeeded = 10;
        public const float PlayMinutesPerDay = 10f;
        public const float GradientSpeed = 0.12f;      // colour cycles per second (0.12 = one full rainbow in ~8 s)

        public static int Gradient { get { return Config.Palette.Length; } }
        public static int ColorCount { get { return Config.Palette.Length + 1; } }
        public static bool IsGradient(int i) { return i == Gradient; }

        public static bool PrimaryUnlocked(int i) { return IsGradient(i) ? SaveManager.Data.longPlayDays >= DaysNeeded : ColorUnlocked(i); }
        public static bool OutlineUnlocked(int i) { return IsGradient(i) ? SaveManager.Data.loginDays >= DaysNeeded : ColorUnlocked(i); }

        /// <summary>Short text on a locked swatch, e.g. "LV 4" or "DAYS 3/10".</summary>
        public static string LockText(int i, bool primary)
        {
            if (!IsGradient(i)) return "LV " + ColorLevel[i];
            var d = SaveManager.Data;
            return primary ? "PLAY " + Mathf.Min(d.longPlayDays, DaysNeeded) + "/" + DaysNeeded
                           : "DAYS " + Mathf.Min(d.loginDays, DaysNeeded) + "/" + DaysNeeded;
        }

        public static string GradientRequirement(bool primary)
        {
            return primary ? "GRADIENT body: play more than " + PlayMinutesPerDay + " minutes on " + DaysNeeded + " different days"
                           : "GRADIENT outline: open the game on " + DaysNeeded + " different days";
        }

        /// <summary>The current colour of the gradient (offset shifts it, so body and outline differ).</summary>
        public static Color GradientColor(float offset = 0f)
        {
            return Color.HSVToRGB(Mathf.Repeat(Time.unscaledTime * GradientSpeed + offset, 1f), 0.78f, 1f);
        }

        static string Today { get { return DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture); } }

        /// <summary>Counts the day the game was opened (call on start and when the app comes back).</summary>
        public static void TrackLogin()
        {
            var d = SaveManager.Data;
            string today = Today;
            if (d.lastLoginDay == today) return;
            d.lastLoginDay = today;
            d.loginDays++;
            SaveManager.Save();
        }

        /// <summary>Adds the time of a finished round; a day counts once the rounds of that day add up to 10 minutes.</summary>
        public static void AddPlayTime(float seconds)
        {
            var d = SaveManager.Data;
            string today = Today;
            if (d.playDay != today) { d.playDay = today; d.playDaySeconds = 0f; d.playDayCounted = false; }
            d.playDaySeconds += seconds;
            if (!d.playDayCounted && d.playDaySeconds >= PlayMinutesPerDay * 60f) { d.playDayCounted = true; d.longPlayDays++; }
        }

        /// <summary>Names of gradient colours unlocked since the last call (shown in the result dialog).</summary>
        public static List<string> CollectNewColours()
        {
            var d = SaveManager.Data;
            var list = new List<string>();
            if (!d.gradPrimaryAnnounced && PrimaryUnlocked(Gradient)) { d.gradPrimaryAnnounced = true; list.Add("Gradient body colour"); }
            if (!d.gradOutlineAnnounced && OutlineUnlocked(Gradient)) { d.gradOutlineAnnounced = true; list.Add("Gradient outline colour"); }
            return list;
        }

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
