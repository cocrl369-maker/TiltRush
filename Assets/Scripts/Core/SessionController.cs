using UnityEngine;

namespace TiltRush
{
    /// <summary>
    /// Runs a whole play session: solo run, local duel or online best-of-N match.
    /// Handles rounds, scores, Elo and the dialogs between rounds.
    /// </summary>
    public class SessionController
    {
        readonly App app;
        ModeDef mode;
        int[] wins = new int[2];
        int round, ties, token;
        MatchInfo match;
        LevelDef level;
        int levelIndex;
        GameWorld world;

        public string ScoreLine { get; private set; }

        public SessionController(App a) { app = a; ScoreLine = ""; }

        // ------------------------------------------------------------------ flow
        public void Start(ModeDef m)
        {
            if (m.Id == GameModeId.Campaign)      // the level select screen normally does this
            {
                StartCampaign(Mathf.Clamp(SaveManager.Data.campaignDone, 0, Campaign.Levels.Length - 1));
                return;
            }
            Abort();
            mode = m; level = null; round = 0; ties = 0; match = null; wins = new int[2];
            if (m.Id == GameModeId.Online) Matchmake(); else BeginRound();
        }

        public void StartCampaign(int index)
        {
            Abort();
            mode = Modes.Campaign; level = Campaign.Levels[index]; levelIndex = index;
            round = 0; ties = 0; match = null; wins = new int[2];
            BeginRound();
        }

        /// <summary>The guided tutorial (runs automatically on the very first start).</summary>
        public void StartTutorial()
        {
            Abort();
            mode = Modes.Tutorial; level = null; round = 0; ties = 0; match = null; wins = new int[2];
            BeginRound();
        }

        /// <summary>SKIP button in the tutorial.</summary>
        public void SkipTutorial()
        {
            SaveManager.Data.tutorialDone = true;
            SaveManager.Save();
            Abort();
            app.UI.Show<MainMenuScreen>();
        }

        public void Abort()
        {
            token++;                       // invalidates pending callbacks
            if (world != null) Object.Destroy(world.gameObject);
            world = null;
            Time.timeScale = 1f;
            ScoreLine = "";
        }

        void Matchmake()
        {
            int t = token;
            app.UI.ShowDialog("SEARCHING...", "Looking for an opponent near ELO " + SaveManager.Data.elo,
                new UIStack.Item("CANCEL", Neon.Gray, () => { Abort(); app.UI.Show<MainMenuScreen>(); }));

            app.Online.FindMatch(SaveManager.Data.elo,
                mi => { if (t != token) return; match = mi; BeginRound(); },
                err => { if (t != token) return; Error(err); });
        }

        void BeginRound()
        {
            UpdateScore();
            app.UI.Show<HudScreen>();
            int seed = match != null ? match.Seed * 31 + round + ties * 7 : Random.Range(1, int.MaxValue);
            int t = token;
            world = GameWorld.Create(mode, seed, r => { if (t == token) OnRoundEnd(r); }, level);
        }

        void OnRoundEnd(RoundResult r)
        {
            Object.Destroy(world.gameObject);
            world = null;
            int t = token;

            if (mode.Id == GameModeId.Tutorial) { EndTutorial(); return; }

            // statistics (skins are unlocked with these)
            var stats = SaveManager.Data;
            stats.runsPlayed++;
            stats.totalKills += r.Kills;
            stats.totalPickups += r.Pickups;
            foreach (float s in r.Survival) stats.totalPlaySeconds = stats.totalPlaySeconds + s;
            SaveManager.Save();

            switch (mode.Id)
            {
                case GameModeId.Campaign:
                    EndCampaign(r);
                    break;

                case GameModeId.Classic:
                case GameModeId.Hard:
                case GameModeId.Dodge:
                    EndSolo(r.Survival[0], r.Points);
                    break;

                case GameModeId.Coop:
                    FinishRound(r.Winner, "P1: " + r.Survival[0].ToString("0.0") + "s\nP2: " + r.Survival[1].ToString("0.0") + "s");
                    break;

                case GameModeId.Online:
                    float mine = r.Survival[0];
                    app.UI.ShowDialog("ROUND " + (round + 1), "You survived " + mine.ToString("0.0") + "s\n\nWaiting for opponent...");
                    app.Online.SubmitRound(match.MatchId, round, mine,
                        opp =>
                        {
                            if (t != token) return;
                            int w = Mathf.Abs(mine - opp) < 0.02f ? -1 : (mine > opp ? 0 : 1);
                            FinishRound(w, "You: " + mine.ToString("0.0") + "s\n" + match.Opponent.Name + ": " + opp.ToString("0.0") + "s");
                        },
                        err => { if (t == token) Error(err); });
                    break;
            }
        }

        void EndTutorial()
        {
            SaveManager.Data.tutorialDone = true;
            SaveManager.Save();
            app.UI.ShowDialog("TUTORIAL COMPLETE!",
                "You survived the tutorial.\nStatistically, that's 100% of the people who finished it.\n\nNext stop: the CAMPAIGN.",
                new UIStack.Item("CAMPAIGN", Neon.Lime, () => app.UI.Show<CampaignScreen>()),
                new UIStack.Item("MENU", Neon.Gray, () => { Abort(); app.UI.Show<MainMenuScreen>(); }));
        }

        void EndCampaign(RoundResult r)
        {
            int number = levelIndex + 1;
            int idx = levelIndex;

            if (r.Completed)
            {
                var d = SaveManager.Data;
                var rewards = new System.Collections.Generic.List<string>();
                if (d.campaignDone < number)                 // first time: progress + unlocks
                {
                    d.campaignDone = number;
                    rewards.AddRange(Unlocks.ColoursFor(number));
                }
                rewards.AddRange(Skins.CollectNew());
                SaveManager.Save();
                string body = "You survived " + level.Duration.ToString("0") + " seconds." +
                              (rewards.Count > 0 ? "\n\nUNLOCKED: " + string.Join(", ", rewards.ToArray()) : "");

                if (idx + 1 < Campaign.Levels.Length)
                    app.UI.ShowDialog("LEVEL COMPLETE!", body,
                        new UIStack.Item("NEXT LEVEL", Neon.Lime, () => StartCampaign(idx + 1)),
                        new UIStack.Item("LEVELS", Neon.Blue, () => app.UI.Show<CampaignScreen>()),
                        new UIStack.Item("MENU", Neon.Gray, () => { Abort(); app.UI.Show<MainMenuScreen>(); }));
                else
                    app.UI.ShowDialog("CAMPAIGN COMPLETE!", body,
                        new UIStack.Item("LEVELS", Neon.Blue, () => app.UI.Show<CampaignScreen>()),
                        new UIStack.Item("MENU", Neon.Gray, () => { Abort(); app.UI.Show<MainMenuScreen>(); }));
            }
            else
            {
                app.UI.ShowDialog("TRY AGAIN",
                    "Level " + number + " - " + level.Name + "\nYou survived " + r.Survival[0].ToString("0.0") + " of " + level.Duration.ToString("0") + " seconds." + NewSkinsText(),
                    new UIStack.Item("RETRY", Neon.Lime, () => StartCampaign(idx)),
                    new UIStack.Item("LEVELS", Neon.Blue, () => app.UI.Show<CampaignScreen>()),
                    new UIStack.Item("MENU", Neon.Gray, () => { Abort(); app.UI.Show<MainMenuScreen>(); }));
            }
        }

        void EndSolo(float time, int score)
        {
            var d = SaveManager.Data;
            int bestScore;
            switch (mode.Id)
            {
                case GameModeId.Hard:  bestScore = d.bestHardScore; break;
                case GameModeId.Dodge: bestScore = d.bestDodgeScore; break;
                default:               bestScore = d.bestClassicScore; break;
            }
            bool isBest = score > bestScore;
            switch (mode.Id)
            {
                case GameModeId.Hard:  d.bestHard = Mathf.Max(d.bestHard, time); if (isBest) d.bestHardScore = score; break;
                case GameModeId.Dodge: d.bestDodge = Mathf.Max(d.bestDodge, time); if (isBest) d.bestDodgeScore = score; break;
                default:               d.bestClassic = Mathf.Max(d.bestClassic, time); if (isBest) d.bestClassicScore = score; break;
            }
            SaveManager.Save();

            string title = isBest ? "NEW BEST!" : "GAME OVER";
            string body = "Score: " + score.ToString("N0") + "\nTime: " + time.ToString("0.0") + "s\nBest score: " + Mathf.Max(bestScore, score).ToString("N0") + NewSkinsText();

            System.Action<string> show = extra =>
            {
                if (mode.Id == GameModeId.Classic)
                    app.UI.ShowDialog(title, body + extra,
                        new UIStack.Item("RETRY", Neon.Lime, () => Start(mode)),
                        new UIStack.Item("LEADERBOARD", Neon.Orange, () => app.UI.Show<LeaderboardScreen>()),
                        new UIStack.Item("MENU", Neon.Gray, () => { Abort(); app.UI.Show<MainMenuScreen>(); }));
                else
                    app.UI.ShowDialog(title, body + extra,
                        new UIStack.Item("RETRY", Neon.Lime, () => Start(mode)),
                        new UIStack.Item("MENU", Neon.Gray, () => { Abort(); app.UI.Show<MainMenuScreen>(); }));
            };
            show("");

            if (mode.Id == GameModeId.Classic)
            {
                int t = token;
                app.Online.SubmitScore("classic", d.playerName, score,
                    rank => { if (t == token && rank > 0 && app.UI.Current is DialogScreen) show("\nWorld rank: #" + rank); },
                    err => { });
            }
        }

        void FinishRound(int winner, string detail)
        {
            if (winner < 0)
            {
                ties++;
                app.UI.ShowDialog("DRAW", detail + "\n\nSame time - round is replayed.",
                    new UIStack.Item("NEXT ROUND", Neon.Lime, BeginRound));
                return;
            }

            wins[winner]++; round++;
            UpdateScore();
            int need = Mathf.Max(1, mode.BestOf / 2 + 1);

            if (wins[winner] >= need) { EndMatch(winner, detail); return; }

            app.UI.ShowDialog(PlayerName(winner) + " WINS THE ROUND", detail + "\n\nScore  " + wins[0] + " : " + wins[1] + NewSkinsText(),
                new UIStack.Item("NEXT ROUND", Neon.Lime, BeginRound));
        }

        void EndMatch(int winner, string detail)
        {
            string score = "Score  " + wins[0] + " : " + wins[1];

            if (mode.Id == GameModeId.Online)
            {
                var d = SaveManager.Data;
                bool won = winner == 0;
                int delta = EloCalculator.Delta(d.elo, match.Opponent.Elo, won);
                int old = d.elo;
                d.elo = Mathf.Max(0, d.elo + delta);
                if (won) d.onlineWins++; else d.onlineLosses++;
                SaveManager.Save();
                app.Online.ReportMatch(match.MatchId, won, d.elo);

                app.UI.ShowDialog(won ? "VICTORY!" : "DEFEAT",
                    score + "\n\nELO " + old + " -> " + d.elo + "  (" + (delta >= 0 ? "+" : "") + delta + ")" + NewSkinsText(),
                    new UIStack.Item("PLAY AGAIN", Neon.Pink, () => Start(mode)),
                    new UIStack.Item("MENU", Neon.Gray, () => { Abort(); app.UI.Show<MainMenuScreen>(); }));
            }
            else
            {
                app.UI.ShowDialog(PlayerName(winner) + " WINS!", detail + "\n\n" + score + NewSkinsText(),
                    new UIStack.Item("REMATCH", Neon.Aqua, () => Start(mode)),
                    new UIStack.Item("MENU", Neon.Gray, () => { Abort(); app.UI.Show<MainMenuScreen>(); }));
            }
        }

        /// <summary>"NEW SKIN: SABER" line for the result dialogs (empty if nothing new). Marks the skins as announced.</summary>
        static string NewSkinsText()
        {
            var list = Skins.CollectNew();
            if (list.Count == 0) return "";
            SaveManager.Save();
            return "\n\n<color=#7DFF00>UNLOCKED: " + string.Join(", ", list.ToArray()).ToUpper() + "</color>";
        }

        void Error(string msg)
        {
            app.UI.ShowDialog("CONNECTION ERROR", msg,
                new UIStack.Item("MENU", Neon.Gray, () => { Abort(); app.UI.Show<MainMenuScreen>(); }));
        }

        string PlayerName(int i)
        {
            if (mode.Id == GameModeId.Online) return i == 0 ? "YOU" : match.Opponent.Name.ToUpper();
            return "PLAYER " + (i + 1);
        }

        void UpdateScore()
        {
            if (mode.Id == GameModeId.Campaign) { ScoreLine = "LEVEL " + (levelIndex + 1) + " - " + level.Name.ToUpper(); return; }
            if (mode.Id == GameModeId.Tutorial) { ScoreLine = "TUTORIAL"; return; }
            if (mode.Players == 1 && mode.Id != GameModeId.Online) { ScoreLine = ""; return; }
            string a = mode.Id == GameModeId.Online ? "YOU" : "P1";
            string b = mode.Id == GameModeId.Online ? (match != null ? match.Opponent.Name : "OPP") : "P2";
            ScoreLine = a + " " + wins[0] + " : " + wins[1] + " " + b + (mode.BestOf > 1 ? "   (Round " + (round + 1) + ")" : "");
        }
    }
}
