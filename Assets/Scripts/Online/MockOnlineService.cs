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

        static List<ScoreEntry> fake;

        /// <summary>Fixed, made-up players (same every run) so the board looks populated.</summary>
        static List<ScoreEntry> Fake()
        {
            if (fake != null) return fake;
            var rnd = new System.Random(42);
            fake = new List<ScoreEntry>();
            for (int i = 0; i < 40; i++)
                fake.Add(new ScoreEntry
                {
                    Name = Names[i % Names.Length] + (i >= Names.Length ? (i * 7 % 90 + 10).ToString() : ""),
                    Score = 100 + (int)(8000 * Math.Pow(rnd.NextDouble(), 2.0))
                });
            return fake;
        }

        public void SubmitScore(string mode, string playerName, int score, Action<int> onRank, Action<string> onError)
        {
            App.I.StartCoroutine(Co());
            IEnumerator Co()
            {
                yield return new WaitForSecondsRealtime(0.4f);
                int rank = 1;
                foreach (var e in Fake()) if (e.Score > score) rank++;
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
                foreach (var e in Fake()) all.Add(new ScoreEntry { Name = e.Name, Score = e.Score });
                var d = SaveManager.Data;
                if (d.bestClassicScore > 0) all.Add(new ScoreEntry { Name = d.playerName, Score = d.bestClassicScore, IsYou = true });
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
