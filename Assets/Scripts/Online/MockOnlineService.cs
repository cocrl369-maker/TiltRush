using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace TiltRush
{
    /// <summary>Offline stand-in with simulated opponents / players so the whole online flow can be tested.</summary>
    public class MockOnlineService : IOnlineService
    {
        int opponentElo;

        public string Status { get { return "OFFLINE (simulated players)"; } }
        public string PlayerId { get { return "-"; } }
        public void LeaveMatch() { }

        // ---------------------------------------------------------------- PvP
        public void FindMatch(int myElo, Action<MatchInfo> onFound, Action<string> onError)
        {
            App.I.StartCoroutine(Co());
            IEnumerator Co()
            {
                yield return new WaitForSecondsRealtime(Random.Range(1f, 2.5f));
                opponentElo = Mathf.Max(0, myElo + Random.Range(-60, 60));
                onFound(new MatchInfo
                {
                    MatchId = Guid.NewGuid().ToString(),
                    Seed = Random.Range(1, 100000),
                    Opponent = new OpponentInfo { Name = "Player#" + Random.Range(1000, 9999), Elo = opponentElo }
                });
            }
        }

        public void SubmitRound(string matchId, int round, float myTime, Action<float> onOpponentTime, Action<string> onError)
        {
            App.I.StartCoroutine(Co());
            IEnumerator Co()
            {
                yield return new WaitForSecondsRealtime(0.7f);
                float mean = 15f + opponentElo * 0.05f;          // ELO 400 -> ~35 s
                onOpponentTime(mean * Random.Range(0.5f, 1.5f));
            }
        }

        public void ReportMatch(string matchId, bool won, int newElo) { }

        // ---------------------------------------------------------------- Leaderboard
        static readonly string[] Names =
        {
            "NovaX", "Glitch", "PixelPilot", "Zed", "Mira", "Orbit", "Kairo", "Lumen", "Vex", "Turbo",
            "Echo", "Blitz", "Sora", "Nyx", "Rift", "Juno", "Axel", "Kilo", "Dash", "Flux"
        };

        static readonly Dictionary<string, List<ScoreEntry>> fake = new Dictionary<string, List<ScoreEntry>>();

        /// <summary>Fixed, made-up players per board (same every run) so the boards look populated.</summary>
        static List<ScoreEntry> Fake(string board)
        {
            List<ScoreEntry> list;
            if (fake.TryGetValue(board, out list)) return list;
            var rnd = new System.Random(board.GetHashCode() & 0x7fffffff);
            bool elo = board == "online";
            double top = board == "hard" ? 6000 : board == "dodge" ? 5000 : board == "asteroids" ? 9000 : 8000;
            list = new List<ScoreEntry>();
            for (int i = 0; i < 40; i++)
                list.Add(new ScoreEntry
                {
                    Name = Names[(i + rnd.Next(Names.Length)) % Names.Length] + (i >= Names.Length / 2 ? (rnd.Next(90) + 10).ToString() : ""),
                    Score = elo ? 250 + (int)(700 * Math.Pow(rnd.NextDouble(), 1.5)) : 100 + (int)(top * Math.Pow(rnd.NextDouble(), 2.0))
                });
            fake[board] = list;
            return list;
        }

        public void SubmitScore(string mode, string playerName, int score, Action<int> onRank, Action<string> onError)
        {
            App.I.StartCoroutine(Co());
            IEnumerator Co()
            {
                yield return new WaitForSecondsRealtime(0.4f);
                int rank = 1;
                foreach (var e in Fake(mode)) if (e.Score > score) rank++;
                onRank(rank);
            }
        }

        public void GetLeaderboard(string mode, int count, Action<List<ScoreEntry>> onResult, Action<string> onError)
        {
            App.I.StartCoroutine(Co());
            IEnumerator Co()
            {
                yield return new WaitForSecondsRealtime(0.5f);

                var all = new List<ScoreEntry>();
                foreach (var e in Fake(mode)) all.Add(new ScoreEntry { Name = e.Name, Score = e.Score });
                int mine = Boards.MyBest(mode);
                if (mine > 0) all.Add(new ScoreEntry { Name = SaveManager.Data.playerName, Score = mine, IsYou = true });
                all.Sort((a, b) => b.Score.CompareTo(a.Score));
                for (int i = 0; i < all.Count; i++) all[i].Rank = i + 1;

                var result = all.GetRange(0, Math.Min(count, all.Count));
                var you = all.Find(e => e.IsYou);
                if (you != null && !result.Contains(you)) result.Add(you);
                onResult(result);
            }
        }
    }
}
