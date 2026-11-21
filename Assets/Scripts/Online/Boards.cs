using UnityEngine;

namespace TiltRush
{
    /// <summary>
    /// The leaderboards. The ID must exist with exactly this name in the Unity Cloud dashboard (Leaderboards).
    /// Campaign and Co-op have no leaderboard.
    /// </summary>
    public static class Boards
    {
        public class Board { public string Id, Title; public Color Color; public bool Elo; }

        public static readonly Board[] All =
        {
            new Board { Id = "classic",   Title = "CLASSIC",   Color = Neon.Lime },
            new Board { Id = "hard",      Title = "HARD",      Color = Neon.Red },
            new Board { Id = "dodge",     Title = "DODGE",     Color = Neon.Pink },
            new Board { Id = "asteroids", Title = "ASTEROIDS", Color = Neon.Orange },
            new Board { Id = "online",    Title = "ONLINE",    Color = Neon.Aqua, Elo = true },   // ranked by ELO
        };

        /// <summary>Leaderboard ID of a mode, or null (campaign, co-op, tutorial).</summary>
        public static string IdFor(GameModeId m)
        {
            switch (m)
            {
                case GameModeId.Classic:   return "classic";
                case GameModeId.Hard:      return "hard";
                case GameModeId.Dodge:     return "dodge";
                case GameModeId.Asteroids: return "asteroids";
                case GameModeId.Online:    return "online";
                default:                   return null;
            }
        }

        public static Board Get(string id)
        {
            foreach (var b in All) if (b.Id == id) return b;
            return All[0];
        }

        /// <summary>Takes over a better score from the leaderboard into the save file (see UgsOnlineService.SyncBests).</summary>
        public static void SetMyBest(string id, int value)
        {
            var d = SaveManager.Data;
            switch (id)
            {
                case "hard":      d.bestHardScore = value; break;
                case "dodge":     d.bestDodgeScore = value; break;
                case "asteroids": d.bestAsteroidsScore = value; break;
                case "classic":   d.bestClassicScore = value; break;
            }
        }

        /// <summary>Your own best value for a board (from the save file).</summary>
        public static int MyBest(string id)
        {
            var d = SaveManager.Data;
            switch (id)
            {
                case "hard":      return d.bestHardScore;
                case "dodge":     return d.bestDodgeScore;
                case "asteroids": return d.bestAsteroidsScore;
                case "online":    return d.onlineWins + d.onlineLosses > 0 ? d.elo : 0;
                default:          return d.bestClassicScore;
            }
        }
    }
}
