using System;
using System.Collections.Generic;
using UnityEngine;

namespace TiltRush
{
    public struct RoundResult
    {
        public float[] Survival;   // seconds survived per player
        public int Points;         // score of the round (solo modes)
        public bool Completed;     // campaign: level goal reached
        public int Winner;         // 0/1, or -1 for a draw
    }

    /// <summary>
    /// One round of play: arena, players, enemies, hazards, pickups, rings, obstacles.
    /// Created by SessionController, destroyed when the round is over.
    /// Endless modes spawn by difficulty; campaign levels (LevelDef) follow a fixed plan.
    /// Separate seeded RNGs (dots / walls / swords / pickups / rings) make rounds with the same seed identical
    /// no matter what the players do - that is what makes the online "same map" race fair.
    /// </summary>
    public class GameWorld : MonoBehaviour
    {
        public enum Phase { Countdown, Playing, Ending, Done }

        public static GameWorld Current { get; private set; }
        public static event Action<string, Color> Toast;   // HUD listens to this
        public static event Action<int> Bonus;             // HUD listens to this (bonus points earned)

        public ModeDef Mode { get; private set; }
        public LevelDef Level { get; private set; }        // null in the endless modes
        public Phase State { get; private set; }
        public bool Paused { get; private set; }

        public readonly List<PlayerController> Players = new List<PlayerController>();
        public readonly List<Enemy> Enemies = new List<Enemy>();
        readonly List<Pickup> pickups = new List<Pickup>();
        readonly List<LaunchRing> rings = new List<LaunchRing>();
        readonly List<Wall> walls = new List<Wall>();
        readonly List<Sword> swords = new List<Sword>();
        readonly List<Obstacle> obstacles = new List<Obstacle>();
        readonly List<Seeker> seekers = new List<Seeker>();
        readonly List<Turret> turrets = new List<Turret>();
        readonly List<Decoy> decoys = new List<Decoy>();
        readonly Stack<Enemy> enemyPool = new Stack<Enemy>();   // killed dots are reused instead of destroyed

        public float Elapsed, Difficulty, CountdownLeft = 3f, Score;
        public float SlowTimer, FreezeTimer;
        public int ScoreInt { get { return Mathf.FloorToInt(Score); } }
        public float TimeLeft { get { return Level != null ? Mathf.Max(0f, Level.Duration - Elapsed) : 0f; } }
        public float EnemyTimeScale { get; private set; }

        Action<RoundResult> onEnd;
        System.Random spawnRng, pickupRng, ringRng, wallRng, swordRng;
        float spawnTimer = 1.0f, pickupTimer = 4f, wallTimer = 3f, swordTimer = 3f, endTimer, areaScale = 1f;
        bool wallsAnnounced, swordsAnnounced, ringsSpawned, gadgetsOn, completed;
        float[] deathTime;
        DodgeDirector director;      // DODGE mode only

        // campaign plan
        List<EnemyType> plan;
        float[] planTimes, wallTimes, swordTimes;
        int planIdx, wallIdx, swordIdx;

        public static GameWorld Create(ModeDef mode, int seed, Action<RoundResult> onEnd, LevelDef level = null)
        {
            var go = new GameObject("GameWorld");
            var w = go.AddComponent<GameWorld>();
            w.Init(mode, seed, onEnd, level);
            return w;
        }

        void Init(ModeDef mode, int seed, Action<RoundResult> onEnd, LevelDef level)
        {
            Current = this; Mode = mode; Level = level; this.onEnd = onEnd;
            Time.timeScale = 1f;
            EnemyTimeScale = 1f;
            Difficulty = level != null ? level.DiffStart : mode.StartDifficulty;
            gadgetsOn = mode.Gadgets && (level == null || level.Gadgets);
            spawnRng = new System.Random(seed);
            pickupRng = new System.Random(seed + 1);
            ringRng = new System.Random(seed + 2);
            wallRng = new System.Random(seed + 3);
            swordRng = new System.Random(seed + 4);
            App.I.FitArena(mode.Id == GameModeId.Online);   // online: identical arena size on every phone
            areaScale = Mathf.Clamp(Config.ArenaSize.x * Config.ArenaSize.y / 150f, 1f, 2.5f);   // bigger arena -> more dots

            ArenaView.Build(transform);

            for (int i = 0; i < mode.Players; i++)
            {
                var go = new GameObject("Player" + i);
                go.transform.SetParent(transform, false);
                var p = go.AddComponent<PlayerController>();
                Vector2 pos = mode.Players == 1 ? Vector2.zero : new Vector2(i == 0 ? -4f : 4f, 0f);
                p.Init(i, InputFactory.Create(i, mode.Players), pos, PlayerStyle.FromSave(i));
                Players.Add(p);
            }
            deathTime = new float[mode.Players];
            if (mode.Id == GameModeId.Dodge) director = new DodgeDirector(transform, new System.Random(seed + 5), (text, col) => Toast?.Invoke(text, col));

            if (level != null)
            {
                foreach (var def in level.Obstacles) obstacles.Add(Obstacle.Create(transform, def, Config.ArenaSize));
                BuildPlan();
                if (level.Rings) SpawnRings(false);
            }
        }

        // ------------------------------------------------------------------ campaign plan
        void BuildPlan()
        {
            plan = new List<EnemyType>();
            for (int i = 0; i < Level.Normal; i++) plan.Add(EnemyType.Normal);
            for (int i = 0; i < Level.Fast; i++) plan.Add(EnemyType.Fast);
            for (int i = 0; i < Level.Big; i++) plan.Add(EnemyType.Big);
            for (int i = plan.Count - 1; i > 0; i--)                   // shuffle (Fisher-Yates)
            {
                int j = spawnRng.Next(i + 1);
                EnemyType tmp = plan[i]; plan[i] = plan[j]; plan[j] = tmp;
            }

            int n = plan.Count;
            planTimes = new float[n];
            for (int i = 0; i < n; i++)
                planTimes[i] = Level.Duration * 0.03f + (i + (float)spawnRng.NextDouble() * 0.8f) / Mathf.Max(1, n) * Level.Duration * 0.85f;

            wallTimes = Spread(Level.Walls, 0.2f, 0.8f);
            swordTimes = Spread(Level.Swords, 0.3f, 0.9f);
        }

        float[] Spread(int count, float from, float to)
        {
            var t = new float[count];
            for (int i = 0; i < count; i++) t[i] = Level.Duration * Mathf.Lerp(from, to, (i + 0.5f) / count);
            return t;
        }

        void CampaignSpawns()
        {
            while (planIdx < plan.Count && Elapsed >= planTimes[planIdx]) SpawnEnemy(plan[planIdx++]);
            while (wallIdx < wallTimes.Length && Elapsed >= wallTimes[wallIdx]) { wallIdx++; SpawnWall(); }
            while (swordIdx < swordTimes.Length && Elapsed >= swordTimes[swordIdx]) { swordIdx++; SpawnSwords(); }
        }

        // ------------------------------------------------------------------ rings
        float RingLife(System.Random r) { return Config.LaunchRingLife * (0.8f + (float)r.NextDouble() * 0.4f); }

        /// <summary>In the endless modes launch rings are demanding, so they only appear once the round is a minute old.</summary>
        void SpawnRings(bool announce)
        {
            ringsSpawned = true;
            if (announce) Toast?.Invoke("LAUNCH RINGS!", Neon.Yellow);
            for (int i = 0; i < Config.LaunchRingsPerPlayer * Mode.Players; i++)
                rings.Add(LaunchRing.Create(transform, RandomPoint(ringRng, 1.5f), RingLife(ringRng) * (0.4f + 0.2f * i)));
        }

        // ------------------------------------------------------------------ main loop
        void Update()
        {
            float dt = Time.deltaTime;
            switch (State)
            {
                case Phase.Countdown:
                    CountdownLeft -= dt;
                    if (CountdownLeft <= 0f)
                    {
                        foreach (var p in Players) p.Controls.Calibrate();
                        State = Phase.Playing;
                    }
                    break;
                case Phase.Playing:
                    // split long frames into small steps (no tunnelling), cap extreme hiccups
                    float total = Mathf.Min(dt, Config.MaxFrameTime);
                    int steps = Mathf.Max(1, Mathf.CeilToInt(total / Config.MaxSubStep - 0.001f));
                    for (int i = 0; i < steps && State == Phase.Playing; i++) Step(total / steps);
                    break;
                case Phase.Ending:
                    endTimer -= dt;
                    if (endTimer <= 0f) { State = Phase.Done; onEnd?.Invoke(BuildResult()); }
                    break;
            }
        }

        void Step(float dt)
        {
            Elapsed += dt;
            Difficulty = Level != null
                ? Mathf.Lerp(Level.DiffStart, Level.DiffEnd, Mathf.Clamp01(Elapsed / Level.Duration))
                : Mathf.Clamp01(Mode.StartDifficulty + Elapsed / (Mode.RampSeconds > 0f ? Mode.RampSeconds : Config.DifficultyRampSeconds));
            NeonBackground.Intensity = Difficulty;
            Score += dt * Config.ScorePerSecond * (1f + Difficulty * Config.ScoreDifficultyBonus);
            if (Level == null && Mode.LaunchRings && !ringsSpawned && Elapsed >= Config.LaunchRingStartTime) SpawnRings(true);

            SlowTimer = Mathf.Max(0f, SlowTimer - dt);
            FreezeTimer = Mathf.Max(0f, FreezeTimer - dt);
            EnemyTimeScale = FreezeTimer > 0f ? 0f : (SlowTimer > 0f ? Config.SlowMoScale : 1f);
            float edt = dt * EnemyTimeScale;

            foreach (var p in Players) p.Tick(dt);

            if (director != null) director.Tick(dt, Difficulty, FirstAlivePos());     // DODGE: patterns instead of chasing dots
            else if (Level == null) SpawnTick(dt);                                     // spawn timers use real time so online players see the same spawns
            else CampaignSpawns();
            foreach (var e in Enemies) e.Tick(edt, dt, this);
            if (Level == null && director == null) HazardSpawnTick(dt);
            HazardMove(dt, edt);
            CheckCollisions();
            CheckHazards();
            GadgetTick(dt);

            if (gadgetsOn) PickupTick(dt);
            for (int i = 0; i < rings.Count; i++)
            {
                rings[i].Tick(dt, this);
                if (rings[i].Life <= 0f) rings[i].Relocate(RandomPoint(ringRng, 1.5f), RingLife(ringRng));
            }

            if (Players.Exists(x => !x.Alive))
            {
                State = Phase.Ending; endTimer = 1.3f;
            }
            else if (Level != null && Elapsed >= Level.Duration)
            {
                completed = true;
                State = Phase.Ending; endTimer = 1.6f;
                Toast?.Invoke("LEVEL COMPLETE!", Neon.Lime);
                KillAllEnemies();
            }
        }

        // ------------------------------------------------------------------ dots
        float SpeedMul { get { return (1f + Difficulty * Config.EnemySpeedGrowth) * Mode.EnemySpeedMul; } }

        void SpawnTick(float dt)
        {
            spawnTimer -= dt;
            if (spawnTimer > 0f) return;
            spawnTimer += Mathf.Lerp(Config.SpawnIntervalStart, Config.SpawnIntervalEnd, Difficulty) / (Mode.SpawnRateMul * areaScale);

            int n = Difficulty > 0.6f ? 2 : 1;
            for (int i = 0; i < n && Enemies.Count < Config.MaxEnemies; i++) SpawnEnemy(PickType());
        }

        void SpawnEnemy(EnemyType type)
        {
            Vector2 pos = EdgePoint();
            float phase = (float)spawnRng.NextDouble() * 6.28f;
            Enemy e = enemyPool.Count > 0 ? enemyPool.Pop() : Enemy.Create(transform);
            e.Setup(type, pos, SpeedMul, phase);
            Enemies.Add(e);
        }

        EnemyType PickType()
        {
            double r = spawnRng.NextDouble();
            if (Difficulty > 0.4f && r < 0.15 * Difficulty) return EnemyType.Big;
            if (Difficulty > 0.2f && r < 0.5 * Difficulty) return EnemyType.Fast;
            return EnemyType.Normal;
        }

        Vector2 EdgePoint()
        {
            Vector2 half = Config.ArenaSize * 0.5f - Vector2.one * 0.2f;
            float u = (float)spawnRng.NextDouble() * 2f - 1f;
            switch (spawnRng.Next(4))
            {
                case 0: return new Vector2(u * half.x, half.y);
                case 1: return new Vector2(u * half.x, -half.y);
                case 2: return new Vector2(-half.x, u * half.y);
                default: return new Vector2(half.x, u * half.y);
            }
        }

        /// <summary>Position the dots chase: the nearest player or decoy (decoys are a bit more attractive).</summary>
        public bool NearestTarget(Vector2 from, out Vector2 target)
        {
            target = Vector2.zero;
            float best = float.MaxValue;
            bool found = false;
            foreach (var p in Players)
            {
                if (!p.Alive) continue;
                float d = (p.Pos - from).sqrMagnitude;
                if (d < best) { best = d; target = p.Pos; found = true; }
            }
            foreach (var dc in decoys)
            {
                float d = (dc.Pos - from).sqrMagnitude * 0.6f;
                if (d < best) { best = d; target = dc.Pos; found = true; }
            }
            return found;
        }

        Vector2 FirstAlivePos()
        {
            foreach (var p in Players) if (p.Alive) return p.Pos;
            return Vector2.zero;
        }

        /// <summary>Nearest dangerous dot within maxDist (used by seekers, turrets, lightning).</summary>
        public Enemy NearestEnemy(Vector2 from, float maxDist)
        {
            Enemy best = null;
            float bd = maxDist * maxDist;
            foreach (var e in Enemies)
            {
                if (e.Dead || !e.Dangerous) continue;
                float d = (e.Pos - from).sqrMagnitude;
                if (d < bd) { bd = d; best = e; }
            }
            return best;
        }

        /// <summary>Pushes a circle out of all obstacles.</summary>
        public Vector2 PushOut(Vector2 p, float r)
        {
            foreach (var o in obstacles) p = o.PushOut(p, r);
            return p;
        }

        void CheckCollisions()
        {
            for (int i = Enemies.Count - 1; i >= 0; i--)
            {
                var e = Enemies[i];
                if (!e.Dangerous) continue;

                bool eaten = false;
                for (int k = decoys.Count - 1; k >= 0; k--)
                {
                    float reach = e.Radius + Decoy.Radius;
                    if ((e.Pos - decoys[k].Pos).sqrMagnitude > reach * reach) continue;
                    decoys[k].Hit();
                    KillEnemy(i, 0f);          // no points for dots that fall for the decoy
                    eaten = true;
                    break;
                }
                if (eaten) continue;

                foreach (var p in Players)
                {
                    if (!p.Alive) continue;
                    float reach = e.Radius + p.Radius * 0.8f;
                    if ((e.Pos - p.Pos).sqrMagnitude > reach * reach) continue;

                    if (p.ShieldTime > 0f) { KillEnemy(i); break; }
                    KillPlayer(p);
                }
            }
        }

        // ------------------------------------------------------------------ walls & swords
        void HazardSpawnTick(float dt)
        {
            if (Difficulty >= Config.WallStartDifficulty)
            {
                wallTimer -= dt;
                if (wallTimer <= 0f)
                {
                    float k = Mathf.InverseLerp(Config.WallStartDifficulty, 1f, Difficulty);
                    wallTimer = Mathf.Lerp(Config.WallIntervalStart, Config.WallIntervalEnd, k) / Mode.HazardRateMul;
                    if (walls.Count < Config.MaxWalls) SpawnWall();
                }
            }

            if (Difficulty >= Config.SwordStartDifficulty)
            {
                swordTimer -= dt;
                if (swordTimer <= 0f)
                {
                    float k = Mathf.InverseLerp(Config.SwordStartDifficulty, 1f, Difficulty);
                    swordTimer = Mathf.Lerp(Config.SwordIntervalStart, Config.SwordIntervalEnd, k) / Mode.HazardRateMul;
                    SpawnSwords();
                }
            }
        }

        void HazardMove(float dt, float edt)
        {
            for (int i = walls.Count - 1; i >= 0; i--)
                if (!walls[i].Tick(edt)) { Destroy(walls[i].gameObject); walls.RemoveAt(i); }
            for (int i = swords.Count - 1; i >= 0; i--)
                if (!swords[i].Tick(dt, edt)) { Destroy(swords[i].gameObject); swords.RemoveAt(i); }
        }

        void SpawnWall()
        {
            if (!wallsAnnounced) { wallsAnnounced = true; Toast?.Invoke("RED WALLS!", Neon.Red); }

            Vector2 half = Config.ArenaSize * 0.5f;
            int side = wallRng.Next(4);
            bool alongX = side < 2;                                   // travels left/right?
            float lateralRange = alongX ? Config.ArenaSize.y : Config.ArenaSize.x;
            float length = lateralRange * Mathf.Lerp(0.3f, 0.5f, (float)wallRng.NextDouble());
            float lateral = ((float)wallRng.NextDouble() * 2f - 1f) * lateralRange * 0.5f;
            float span = alongX ? Config.ArenaSize.x : Config.ArenaSize.y;
            float edge = Config.WallThickness;

            Vector2 dir, start;
            switch (side)
            {
                case 0:  dir = new Vector2(1, 0);  start = new Vector2(-half.x - edge, lateral); break;
                case 1:  dir = new Vector2(-1, 0); start = new Vector2(half.x + edge, lateral); break;
                case 2:  dir = new Vector2(0, 1);  start = new Vector2(lateral, -half.y - edge); break;
                default: dir = new Vector2(0, -1); start = new Vector2(lateral, half.y + edge); break;
            }
            float speed = Config.WallSpeed * (1f + Difficulty * Config.EnemySpeedGrowth * 0.6f) * Mode.EnemySpeedMul;
            walls.Add(Wall.Spawn(transform, start, dir, length, span + 2f * edge, speed));
        }

        void SpawnSwords()
        {
            if (!swordsAnnounced) { swordsAnnounced = true; Toast?.Invoke("SWORDS!", Sword.Marker); }
            App.I.Audio.Play("warn");

            int count = (Difficulty > 0.85f && swordRng.NextDouble() < 0.5) ? 2 : 1;
            Vector2 half = Config.ArenaSize * 0.5f;
            float warning = Mathf.Lerp(Config.SwordWarning, Config.SwordWarning * 0.7f, Difficulty);

            for (int i = 0; i < count; i++)
            {
                double r = swordRng.NextDouble();
                float angle;                                              // lane direction in degrees
                if (r < 0.35) angle = 0f;
                else if (r < 0.6) angle = 90f;
                else angle = (30f + (float)swordRng.NextDouble() * 60f) * (swordRng.NextDouble() < 0.5 ? 1f : -1f);
                angle *= Mathf.Deg2Rad;

                Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                if (swordRng.NextDouble() < 0.5) dir = -dir;
                Vector2 nrm = new Vector2(-dir.y, dir.x);

                float reach = Mathf.Abs(nrm.x) * half.x + Mathf.Abs(nrm.y) * half.y;
                float offset = ((float)swordRng.NextDouble() * 2f - 1f) * reach * 0.9f;
                float l0 = half.magnitude + Config.SwordLength;           // start/end lie outside the screen
                swords.Add(Sword.Spawn(transform, nrm * offset - dir * l0, dir, l0 * 2f, warning, Config.SwordSpeed));
            }
        }

        void CheckHazards()
        {
            foreach (var p in Players)
            {
                if (!p.Alive || p.ShieldTime > 0f) continue;
                float r = p.Radius * 0.8f;
                bool hit = false;
                foreach (var w in walls) if (w.Hits(p.Pos, r)) { hit = true; break; }
                if (!hit) foreach (var s in swords) if (s.Hits(p.Pos, r)) { hit = true; break; }
                if (!hit && director != null && director.Hits(p.Pos, r)) hit = true;
                if (hit) KillPlayer(p);
            }
        }

        void ClearHazards()
        {
            foreach (var w in walls) Destroy(w.gameObject);
            foreach (var s in swords) Destroy(s.gameObject);
            walls.Clear(); swords.Clear();
        }

        // ------------------------------------------------------------------ kills & points
        void KillPlayer(PlayerController p)
        {
            p.Kill();
            App.I.Shake(0.35f, 0.45f);
            deathTime[p.Index] = Elapsed;
            App.I.Audio.Play("die");
        }

        /// <summary>Adds bonus points (scaled by difficulty). Returns the points actually awarded.</summary>
        int AddBonus(int basePoints, float mul = 1f)
        {
            int pts = Mathf.RoundToInt(basePoints * mul * (1f + Difficulty * Config.ScoreDifficultyBonus));
            if (pts <= 0) return 0;
            Score += pts;
            Bonus?.Invoke(pts);
            return pts;
        }

        void KillEnemy(int i, float pointsMul = 1f)
        {
            var e = Enemies[i];
            if (e.Dangerous) AddBonus(Config.KillPoints[(int)e.Type], pointsMul);   // dots still spawning give nothing
            e.Dead = true;
            Enemies.RemoveAt(i);
            Fx.Burst(e.Pos, Neon.Red, 0.1f, 0.5f, 0.3f);
            Fx.Sparks(e.Pos, e.SparkColor, e.Type == EnemyType.Big ? 12 : 7, e.Type == EnemyType.Big ? 6.5f : 5f);
            e.gameObject.SetActive(false);
            enemyPool.Push(e);
        }

        public void KillEnemyObj(Enemy e, float pointsMul = 1f)
        {
            int i = Enemies.IndexOf(e);
            if (i >= 0) KillEnemy(i, pointsMul);
        }

        // ------------------------------------------------------------------ gadgets API
        public void Shockwave(Vector2 center, float radius)
        {
            Fx.Burst(center, Neon.Orange, 0.3f, radius, 0.5f);
            App.I.Shake(0.3f, 0.4f);
            for (int i = Enemies.Count - 1; i >= 0; i--)
                if ((Enemies[i].Pos - center).magnitude < radius) KillEnemy(i);
        }

        public void KillAllEnemies()
        {
            Fx.Burst(Vector2.zero, Color.white, 0.5f, 40f, 0.7f);
            App.I.Shake(0.6f, 0.7f);
            for (int i = Enemies.Count - 1; i >= 0; i--) KillEnemy(i, Config.NukeKillMultiplier);
            ClearHazards();
        }

        public void LaunchSeekers(Vector2 pos, int count)
        {
            for (int i = 0; i < count; i++)
            {
                float a = i / (float)count * 6.2832f;
                seekers.Add(Seeker.Spawn(transform, pos, new Vector2(Mathf.Cos(a), Mathf.Sin(a))));
            }
        }

        /// <summary>Three piercing beams (straight ahead and 25 degrees to each side).</summary>
        public void FireLance(Vector2 origin, Vector2 facing)
        {
            App.I.Shake(0.2f, 0.25f);
            Color beam = new Color32(255, 225, 100, 255);
            float[] angles = { -25f, 0f, 25f };
            foreach (float deg in angles)
            {
                Vector2 d = Quaternion.Euler(0, 0, deg) * facing;
                Fx.Beam(origin, d, 45f, beam);
                for (int i = Enemies.Count - 1; i >= 0; i--)
                {
                    var e = Enemies[i];
                    if (!e.Dangerous) continue;
                    Vector2 rel = e.Pos - origin;
                    if (Vector2.Dot(rel, d) < 0f) continue;                       // behind us
                    float side = Mathf.Abs(rel.x * d.y - rel.y * d.x);            // distance from the beam line
                    if (side < 0.4f + e.Radius) KillEnemy(i);
                }
            }
        }

        /// <summary>Lightning: strikes the nearest dot and jumps on to up to three more.</summary>
        void Lightning(Vector2 from)
        {
            Vector2 cur = from;
            float range = 8f;
            for (int hop = 0; hop < 4; hop++)
            {
                var e = NearestEnemy(cur, range);
                if (e == null) break;
                Vector2 target = e.Pos;
                Fx.Bolt(cur, target, new Color32(160, 150, 255, 255));
                KillEnemyObj(e);
                cur = target;
                range = 3.5f;
            }
        }

        public void PlaceTurret(Vector2 pos) { turrets.Add(Turret.Spawn(transform, pos)); }
        public void PlaceDecoy(Vector2 pos, int playerIndex) { decoys.Add(Decoy.Spawn(transform, pos, PlayerStyle.FromSave(playerIndex))); }

        void GadgetTick(float dt)
        {
            for (int i = seekers.Count - 1; i >= 0; i--)
                if (!seekers[i].Tick(dt, this)) { Destroy(seekers[i].gameObject); seekers.RemoveAt(i); }
            for (int i = turrets.Count - 1; i >= 0; i--)
                if (!turrets[i].Tick(dt, this)) { Destroy(turrets[i].gameObject); turrets.RemoveAt(i); }
            for (int i = decoys.Count - 1; i >= 0; i--)
                if (!decoys[i].Tick(dt)) { Destroy(decoys[i].gameObject); decoys.RemoveAt(i); }

            foreach (var p in Players)
            {
                if (!p.Alive || p.StormTime <= 0f) continue;
                p.StormCd -= dt;
                if (p.StormCd <= 0f) { p.StormCd = 0.3f; Lightning(p.Pos); }
            }
        }

        void PickupTick(float dt)
        {
            pickupTimer -= dt;
            if (pickupTimer <= 0f)
            {
                pickupTimer = Config.GadgetSpawnInterval * (0.7f + (float)pickupRng.NextDouble() * 0.6f);
                pickups.Add(Pickup.Spawn(transform, GadgetLibrary.PickRandom(pickupRng), RandomPoint(pickupRng, 1f)));
            }

            for (int i = pickups.Count - 1; i >= 0; i--)
            {
                var pk = pickups[i];
                bool collected = false;
                foreach (var p in Players)
                {
                    if (!p.Alive) continue;
                    float reach = Pickup.Radius + p.Radius;
                    if ((pk.Pos - p.Pos).sqrMagnitude > reach * reach) continue;

                    pk.Def.Apply(this, p);
                    int pts = AddBonus(Config.GadgetPoints);
                    Toast?.Invoke(pk.Def.Name.ToUpper() + "  +" + pts, pk.Def.Color);
                    Fx.Burst(pk.Pos, pk.Def.Color, 0.3f, 1.8f, 0.4f);
                    Fx.Sparks(pk.Pos, pk.Def.Color, 16, 7f, 0.5f);
                    App.I.Audio.Play("pickup");
                    collected = true;
                    break;
                }
                if (collected || !pk.Tick(dt)) { pickups.RemoveAt(i); Destroy(pk.gameObject); }
            }
        }

        /// <summary>Random point in the arena that is not inside an obstacle (deterministic for a given seed).</summary>
        Vector2 RandomPoint(System.Random r, float margin)
        {
            Vector2 half = Config.ArenaSize * 0.5f - Vector2.one * margin;
            Vector2 p = Vector2.zero;
            for (int tries = 0; tries < 12; tries++)
            {
                p = new Vector2(((float)r.NextDouble() * 2f - 1f) * half.x, ((float)r.NextDouble() * 2f - 1f) * half.y);
                if (PushOut(p, 0.7f) == p) return p;
            }
            return p;
        }

        // ------------------------------------------------------------------ result / pause
        RoundResult BuildResult()
        {
            var res = new RoundResult { Survival = new float[Players.Count], Winner = 0, Points = ScoreInt, Completed = completed };
            for (int i = 0; i < Players.Count; i++)
                res.Survival[i] = Players[i].Alive ? Elapsed : deathTime[i];

            if (Players.Count > 1)
            {
                float diff = res.Survival[0] - res.Survival[1];
                res.Winner = Mathf.Abs(diff) < 0.02f ? -1 : (diff > 0 ? 0 : 1);
            }
            return res;
        }

        public void SetPaused(bool p) { Paused = p; Time.timeScale = p ? 0f : 1f; }

        /// <summary>Unpause. A running round restarts with a short 2-1 countdown (and re-reads the tilt pose).</summary>
        public void Resume()
        {
            SetPaused(false);
            if (State == Phase.Playing) { State = Phase.Countdown; CountdownLeft = 2f; }
        }

        void OnDestroy()
        {
            if (Current == this) Current = null;
            Time.timeScale = 1f;
            NeonBackground.Intensity = 0.1f;
        }
    }
}
