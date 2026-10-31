using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
#if TILTRUSH_LEADERBOARDS || TILTRUSH_LOBBY
using System.Threading.Tasks;
using Unity.Services.Core;
using Unity.Services.Authentication;
#endif
#if TILTRUSH_LEADERBOARDS
using Unity.Services.Leaderboards;
#endif
#if TILTRUSH_LOBBY
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
#endif

namespace TiltRush
{
    /// <summary>Picks the online backend: Unity Gaming Services when its packages are installed, else the simulation.</summary>
    public static class OnlineFactory
    {
        public static IOnlineService Create()
        {
#if TILTRUSH_LEADERBOARDS || TILTRUSH_LOBBY
            return new UgsOnlineService();
#else
            return new MockOnlineService();
#endif
        }
    }

#if TILTRUSH_LEADERBOARDS || TILTRUSH_LOBBY
    /// <summary>
    /// Real online play with Unity Gaming Services (UGS).
    /// - Login: anonymous (automatic, no account needed for the player).
    /// - Leaderboard: package "Leaderboards", leaderboard ID = Config.LeaderboardId ("classic").
    /// - Online PvP: package "Lobby" (or "Multiplayer Services"). Players with a similar ELO meet in a lobby,
    ///   the host sets the seed, both play the same map and write their round times into the lobby.
    ///   If nobody is found within Config.MatchmakingSeconds you play against a bot (the simulation).
    /// Everything that is not available (package missing, no internet, project not linked) falls back to the simulation.
    /// The defines TILTRUSH_LEADERBOARDS / TILTRUSH_LOBBY are set automatically by Assets/Scripts/TiltRush.asmdef.
    /// </summary>
    public class UgsOnlineService : IOnlineService
    {
        readonly MockOnlineService mock = new MockOnlineService();
        Task initTask;
        bool ready;
        string status = "CONNECTING...";
        string syncedName;

        public string Status { get { return status; } }

        public UgsOnlineService() { _ = EnsureReady(); }   // connect right away

        async Task EnsureReady()
        {
            if (ready) return;
            if (initTask == null) initTask = Init();
            await initTask;
        }

        async Task Init()
        {
            try
            {
                await UnityServices.InitializeAsync();
                if (!AuthenticationService.Instance.IsSignedIn) await AuthenticationService.Instance.SignInAnonymouslyAsync();
                ready = true;
                status = "ONLINE";
            }
            catch (Exception e)
            {
                status = "OFFLINE - " + e.Message;
                Debug.LogWarning("TiltRush online: " + e);
            }
        }

        static string MyId { get { return AuthenticationService.Instance.PlayerId; } }

        /// <summary>UGS adds "#1234" to player names - we hide that again.</summary>
        static string CleanName(string n)
        {
            if (string.IsNullOrEmpty(n)) return "Player";
            int h = n.LastIndexOf('#');
            return h > 0 ? n.Substring(0, h) : n;
        }

        // ================================================================== leaderboard
        public async void SubmitScore(string mode, string playerName, int score, Action<int> onRank, Action<string> onError)
        {
#if TILTRUSH_LEADERBOARDS
            await EnsureReady();
            if (!ready) { mock.SubmitScore(mode, playerName, score, onRank, onError); return; }
            try
            {
                await SyncName(playerName);
                var entry = await LeaderboardsService.Instance.AddPlayerScoreAsync(Config.LeaderboardId, score);
                onRank(entry.Rank + 1);
            }
            catch (Exception e) { onError(e.Message); }
#else
            mock.SubmitScore(mode, playerName, score, onRank, onError);
            await Task.CompletedTask;
#endif
        }

        public async void GetLeaderboard(string mode, int count, Action<List<ScoreEntry>> onResult, Action<string> onError)
        {
#if TILTRUSH_LEADERBOARDS
            await EnsureReady();
            if (!ready) { mock.GetLeaderboard(mode, count, onResult, onError); return; }
            try
            {
                var page = await LeaderboardsService.Instance.GetScoresAsync(Config.LeaderboardId);
                var list = new List<ScoreEntry>();
                bool youIn = false;
                foreach (var e in page.Results)
                {
                    if (list.Count >= count) break;
                    bool you = e.PlayerId == MyId;
                    youIn |= you;
                    list.Add(new ScoreEntry { Rank = e.Rank + 1, Name = CleanName(e.PlayerName), Score = (int)e.Score, IsYou = you });
                }
                if (!youIn)
                {
                    try
                    {
                        var me = await LeaderboardsService.Instance.GetPlayerScoreAsync(Config.LeaderboardId);
                        list.Add(new ScoreEntry { Rank = me.Rank + 1, Name = CleanName(me.PlayerName), Score = (int)me.Score, IsYou = true });
                    }
                    catch (Exception) { }   // no score yet
                }
                onResult(list);
            }
            catch (Exception e) { onError(e.Message); }
#else
            mock.GetLeaderboard(mode, count, onResult, onError);
            await Task.CompletedTask;
#endif
        }

        async Task SyncName(string name)
        {
            string clean = string.IsNullOrEmpty(name) ? "Player" : name.Trim().Replace(' ', '_');
            if (clean == syncedName) return;
            try { await AuthenticationService.Instance.UpdatePlayerNameAsync(clean); syncedName = clean; }
            catch (Exception e) { Debug.LogWarning("TiltRush: could not set player name: " + e.Message); }
        }

        // ================================================================== online PvP
        string lobbyId;
        bool isHost, usingBot;
        int search;           // increases on LeaveMatch -> running searches stop

        public async void FindMatch(int myElo, Action<MatchInfo> onFound, Action<string> onError)
        {
#if TILTRUSH_LOBBY
            await EnsureReady();
            usingBot = false;
            if (!ready) { usingBot = true; mock.FindMatch(myElo, onFound, onError); return; }
            int mySearch = ++search;
            float deadline = Time.realtimeSinceStartup + Config.MatchmakingSeconds;

            var pdata = new Dictionary<string, PlayerDataObject>
            {
                { "name", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, SaveManager.Data.playerName) },
                { "elo",  new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, myElo.ToString(CultureInfo.InvariantCulture)) }
            };
            var player = new Player(id: MyId, data: pdata);
            Lobby lobby = null;

            try
            {
                // 1) join a waiting player with a similar ELO
                var q = new QuickJoinLobbyOptions
                {
                    Player = player,
                    Filter = new List<QueryFilter>
                    {
                        new QueryFilter(QueryFilter.FieldOptions.N1, (myElo - 300).ToString(CultureInfo.InvariantCulture), QueryFilter.OpOptions.GE),
                        new QueryFilter(QueryFilter.FieldOptions.N1, (myElo + 300).ToString(CultureInfo.InvariantCulture), QueryFilter.OpOptions.LE)
                    }
                };
                try { lobby = await LobbyService.Instance.QuickJoinLobbyAsync(q); isHost = false; }
                catch (Exception) { lobby = null; }   // nobody waiting
                if (mySearch != search) { if (lobby != null) { lobbyId = lobby.Id; Leave(); } return; }

                // 2) nobody there: open our own lobby and wait
                if (lobby == null)
                {
                    var c = new CreateLobbyOptions
                    {
                        IsPrivate = false,
                        Player = player,
                        Data = new Dictionary<string, DataObject>
                        {
                            { "elo",  new DataObject(DataObject.VisibilityOptions.Public, myElo.ToString(CultureInfo.InvariantCulture), DataObject.IndexOptions.N1) },
                            { "seed", new DataObject(DataObject.VisibilityOptions.Member, UnityEngine.Random.Range(1, 100000).ToString(CultureInfo.InvariantCulture)) }
                        }
                    };
                    lobby = await LobbyService.Instance.CreateLobbyAsync("TiltRush", 2, c);
                    isHost = true; lobbyId = lobby.Id;
                    App.I.StartCoroutine(Heartbeat(lobby.Id));
                    while (lobby.Players.Count < 2)
                    {
                        if (mySearch != search) { Leave(); return; }
                        if (Time.realtimeSinceStartup > deadline)
                        {
                            Leave();
                            usingBot = true;                             // nobody online: play against a bot
                            mock.FindMatch(myElo, mi => { mi.Opponent.Name += " (BOT)"; onFound(mi); }, onError);
                            return;
                        }
                        await Task.Delay(2000);
                        lobby = await LobbyService.Instance.GetLobbyAsync(lobbyId);
                    }
                }
                lobbyId = lobby.Id;
                if (mySearch != search) { Leave(); return; }

                Player opp = lobby.Players.Find(p => p.Id != MyId);
                onFound(new MatchInfo
                {
                    MatchId = lobby.Id,
                    Seed = ParseInt(lobby.Data, "seed", 1),
                    Opponent = new OpponentInfo { Name = Get(opp, "name", "Rival"), Elo = ParseInt(opp, "elo", myElo) }
                });
            }
            catch (Exception e)
            {
                Leave();
                onError(e.Message);
            }
#else
            usingBot = true;
            mock.FindMatch(myElo, onFound, onError);
            await Task.CompletedTask;
#endif
        }

        public async void SubmitRound(string matchId, int round, float myTime, Action<float> onOpponentTime, Action<string> onError)
        {
#if TILTRUSH_LOBBY
            if (usingBot || !ready || lobbyId == null) { mock.SubmitRound(matchId, round, myTime, onOpponentTime, onError); return; }
            int mySearch = search;
            string key = "r" + round;
            try
            {
                var u = new UpdatePlayerOptions
                {
                    Data = new Dictionary<string, PlayerDataObject>
                    {
                        { key, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, myTime.ToString("R", CultureInfo.InvariantCulture)) }
                    }
                };
                await LobbyService.Instance.UpdatePlayerAsync(lobbyId, MyId, u);

                float limit = Time.realtimeSinceStartup + 240f;              // the opponent may still be playing
                while (true)
                {
                    await Task.Delay(1500);
                    if (mySearch != search || lobbyId == null) return;
                    var lobby = await LobbyService.Instance.GetLobbyAsync(lobbyId);
                    Player opp = lobby.Players.Find(p => p.Id != MyId);
                    if (opp == null) { onOpponentTime(0f); return; }        // opponent left: you win the round
                    string v = Get(opp, key, null);
                    if (v != null) { onOpponentTime(float.Parse(v, CultureInfo.InvariantCulture)); return; }
                    if (Time.realtimeSinceStartup > limit) { onOpponentTime(0f); return; }
                }
            }
            catch (Exception e) { onError(e.Message); }
#else
            mock.SubmitRound(matchId, round, myTime, onOpponentTime, onError);
            await Task.CompletedTask;
#endif
        }

        public void ReportMatch(string matchId, bool won, int newElo) { LeaveMatch(); }

        public void LeaveMatch()
        {
            search++;
            Leave();
        }

        void Leave()
        {
#if TILTRUSH_LOBBY
            if (lobbyId == null || !ready) { lobbyId = null; return; }
            string id = lobbyId;
            lobbyId = null;
            try
            {
                if (isHost) LobbyService.Instance.DeleteLobbyAsync(id);
                else LobbyService.Instance.RemovePlayerAsync(id, MyId);
            }
            catch (Exception) { }
#else
            lobbyId = null;
#endif
        }

#if TILTRUSH_LOBBY
        /// <summary>The host has to ping the lobby regularly, otherwise it disappears.</summary>
        IEnumerator Heartbeat(string id)
        {
            while (lobbyId == id && isHost)
            {
                try { LobbyService.Instance.SendHeartbeatPingAsync(id); } catch (Exception) { }
                yield return new WaitForSecondsRealtime(15f);
            }
        }

        static string Get(Player p, string key, string fallback)
        {
            if (p == null || p.Data == null) return fallback;
            PlayerDataObject o;
            return p.Data.TryGetValue(key, out o) && o != null && o.Value != null ? o.Value : fallback;
        }

        static int ParseInt(Player p, string key, int fallback)
        {
            int v;
            return int.TryParse(Get(p, key, null), NumberStyles.Integer, CultureInfo.InvariantCulture, out v) ? v : fallback;
        }

        static int ParseInt(Dictionary<string, DataObject> data, string key, int fallback)
        {
            DataObject o; int v;
            if (data != null && data.TryGetValue(key, out o) && o != null &&
                int.TryParse(o.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out v)) return v;
            return fallback;
        }
#endif
    }
#endif
}
