using System;
using System.Collections.Generic;

namespace TiltRush
{
    public class OpponentInfo { public string Name; public int Elo; }
    public class MatchInfo { public string MatchId; public int Seed; public OpponentInfo Opponent; }
    public class ScoreEntry { public int Rank; public string Name; public int Score; public bool IsYou; }

    /// <summary>
    /// Everything the game needs from a backend. Online PvP works as a "same-seed race": both players
    /// play the identical map (same seed) on their own device, results are compared per round.
    /// Implementations: MockOnlineService (offline, simulated players) and UgsOnlineService (Unity Gaming Services,
    /// switches on automatically when the UGS packages are installed - see README "Online").
    /// </summary>
    public interface IOnlineService
    {
        /// <summary>Short status text for the options screen, e.g. "ONLINE" or "OFFLINE (simulated players)".</summary>
        string Status { get; }

        /// <summary>Online player ID (shown in Options > Profile, needed for data deletion requests). "-" when offline.</summary>
        string PlayerId { get; }

        /// <summary>Stop searching / leave the current match (cancel button, quitting, match over).</summary>
        void LeaveMatch();

        // PvP
        void FindMatch(int myElo, Action<MatchInfo> onFound, Action<string> onError);
        void SubmitRound(string matchId, int round, float myTime, Action<float> onOpponentTime, Action<string> onError);
        void ReportMatch(string matchId, bool won, int newElo);

        // Leaderboard (Classic mode)
        void SubmitScore(string mode, string playerName, int score, Action<int> onRank, Action<string> onError);
        /// <summary>Top 'count' entries; if the player is not among them, their own entry is appended at the end.</summary>
        void GetLeaderboard(string mode, int count, Action<List<ScoreEntry>> onResult, Action<string> onError);
    }
}
