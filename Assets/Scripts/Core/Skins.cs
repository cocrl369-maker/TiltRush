using System.Collections.Generic;
using UnityEngine;

namespace TiltRush
{
    /// <summary>Second-colour detail drawn inside a skin (in the outline colour), like the two-colour icons in GD.</summary>
    public enum SkinDetail { None, Inner, Core }

    /// <summary>What you have to do to unlock a skin.</summary>
    public enum UnlockKind { Free, Campaign, ClassicScore, HardScore, DodgeTime, OnlineWins, Kills, Pickups, Runs }

    public class SkinDef
    {
        public string Name;
        public ShapeType Shape;
        public SkinDetail Detail;
        public UnlockKind Kind;
        public int Value;

        public bool Unlocked { get { return Kind == UnlockKind.Free || Skins.Progress(Kind) >= Value; } }

        /// <summary>Short text for locked buttons, e.g. "LEVEL 4" or "300 DOTS".</summary>
        public string ShortRequirement
        {
            get
            {
                switch (Kind)
                {
                    case UnlockKind.Campaign:     return "LEVEL " + Value;
                    case UnlockKind.ClassicScore: return Value.ToString("N0") + " PTS";
                    case UnlockKind.HardScore:    return "HARD " + Value.ToString("N0");
                    case UnlockKind.DodgeTime:    return "DODGE " + Value + "s";
                    case UnlockKind.OnlineWins:   return Value + " WINS";
                    case UnlockKind.Kills:        return Value.ToString("N0") + " DOTS";
                    case UnlockKind.Pickups:      return Value + " PICKUPS";
                    case UnlockKind.Runs:         return Value + " RUNS";
                    default:                      return "";
                }
            }
        }

        /// <summary>Full sentence with progress, e.g. "Destroy 300 dots in total (120 / 300)".</summary>
        public string Requirement
        {
            get
            {
                if (Kind == UnlockKind.Free) return "Available from the start";
                string p = " (" + Mathf.Min(Skins.Progress(Kind), Value).ToString("N0") + " / " + Value.ToString("N0") + ")";
                switch (Kind)
                {
                    case UnlockKind.Campaign:     return "Complete campaign level " + Value + p;
                    case UnlockKind.ClassicScore: return "Score " + Value.ToString("N0") + " points in CLASSIC" + p;
                    case UnlockKind.HardScore:    return "Score " + Value.ToString("N0") + " points in HARD" + p;
                    case UnlockKind.DodgeTime:    return "Survive " + Value + " seconds in DODGE" + p;
                    case UnlockKind.OnlineWins:   return "Win " + Value + " online matches" + p;
                    case UnlockKind.Kills:        return "Destroy " + Value.ToString("N0") + " dots in total" + p;
                    case UnlockKind.Pickups:      return "Collect " + Value + " pickups in total" + p;
                    default:                      return "Play " + Value + " rounds" + p;
                }
            }
        }
    }

    /// <summary>
    /// All player skins. Add a new skin: add a shape to ShapeType + its polygon in SpriteFactory, then one line here.
    /// Keep the order (the save file stores the index).
    /// </summary>
    public static class Skins
    {
        static SkinDef S(string n, ShapeType s, SkinDetail d, UnlockKind k, int v)
        {
            return new SkinDef { Name = n, Shape = s, Detail = d, Kind = k, Value = v };
        }

        public static readonly SkinDef[] All =
        {
            // the original eight (free / campaign)
            S("ARROW",   ShapeType.Arrow,   SkinDetail.None,  UnlockKind.Free, 0),
            S("DART",    ShapeType.Dart,    SkinDetail.None,  UnlockKind.Free, 0),
            S("KITE",    ShapeType.Kite,    SkinDetail.None,  UnlockKind.Campaign, 2),
            S("SHIP",    ShapeType.Ship,    SkinDetail.None,  UnlockKind.Campaign, 4),
            S("NEEDLE",  ShapeType.Needle,  SkinDetail.None,  UnlockKind.Campaign, 6),
            S("COMET",   ShapeType.Comet,   SkinDetail.None,  UnlockKind.Campaign, 8),
            S("WING",    ShapeType.Wing,    SkinDetail.None,  UnlockKind.Campaign, 10),
            S("FALCON",  ShapeType.Falcon,  SkinDetail.None,  UnlockKind.Campaign, 12),
            // new skins with two colours, unlocked by playing well
            S("ROCKET",  ShapeType.Rocket,  SkinDetail.Core,  UnlockKind.ClassicScore, 1000),
            S("STAR",    ShapeType.Star,    SkinDetail.Inner, UnlockKind.Pickups, 25),
            S("CRYSTAL", ShapeType.Crystal, SkinDetail.Inner, UnlockKind.Runs, 20),
            S("FANG",    ShapeType.Fang,    SkinDetail.Core,  UnlockKind.Kills, 300),
            S("MANTA",   ShapeType.Manta,   SkinDetail.Inner, UnlockKind.DodgeTime, 45),
            S("BOLT",    ShapeType.Bolt,    SkinDetail.None,  UnlockKind.ClassicScore, 3000),
            S("SPEAR",   ShapeType.Spear,   SkinDetail.Inner, UnlockKind.HardScore, 1500),
            S("HORNET",  ShapeType.Hornet,  SkinDetail.Core,  UnlockKind.DodgeTime, 90),
            S("CROWN",   ShapeType.Crown,   SkinDetail.Core,  UnlockKind.OnlineWins, 3),
            S("SABER",   ShapeType.Saber,   SkinDetail.Inner, UnlockKind.ClassicScore, 6000),
            S("PHOENIX", ShapeType.Phoenix, SkinDetail.Core,  UnlockKind.Kills, 1500),
            S("NOVA",    ShapeType.Nova,    SkinDetail.Inner, UnlockKind.HardScore, 4000),
        };

        public static int Progress(UnlockKind k)
        {
            var d = SaveManager.Data;
            switch (k)
            {
                case UnlockKind.Campaign:     return d.campaignDone;
                case UnlockKind.ClassicScore: return d.bestClassicScore;
                case UnlockKind.HardScore:    return d.bestHardScore;
                case UnlockKind.DodgeTime:    return Mathf.FloorToInt(d.bestDodge);
                case UnlockKind.OnlineWins:   return d.onlineWins;
                case UnlockKind.Kills:        return d.totalKills;
                case UnlockKind.Pickups:      return d.totalPickups;
                case UnlockKind.Runs:         return d.runsPlayed;
                default:                      return int.MaxValue;
            }
        }

        public static int UnlockedCount
        {
            get { int n = 0; foreach (var s in All) if (s.Unlocked) n++; return n; }
        }

        /// <summary>Remembers all currently unlocked skins without announcing them (first start / after a reset).</summary>
        public static void MarkAllKnown()
        {
            var d = SaveManager.Data;
            for (int i = 0; i < All.Length; i++) if (All[i].Unlocked) d.knownSkins |= 1L << i;
        }

        /// <summary>Names of skins that were unlocked since the last call ("SABER skin"). Saves the new state.</summary>
        public static List<string> CollectNew()
        {
            var d = SaveManager.Data;
            var list = new List<string>();
            for (int i = 0; i < All.Length; i++)
            {
                long bit = 1L << i;
                if ((d.knownSkins & bit) != 0 || !All[i].Unlocked) continue;
                d.knownSkins |= bit;
                list.Add(All[i].Name + " skin");
            }
            return list;
        }
    }
}
