using System;
using System.Collections.Generic;
using UnityEngine;

namespace TiltRush
{
    /// <summary>
    /// A gadget = data + one Apply action. To add a new gadget just add an entry to GadgetLibrary.All;
    /// it will automatically spawn in-game and show up in the "How to play" screen.
    /// </summary>
    public class GadgetDef
    {
        public string Name, Description;
        public Color Color;
        public ShapeType Icon;
        public float Weight = 1f;   // spawn probability weight
        public Action<GameWorld, PlayerController> Apply;
    }

    public static class GadgetLibrary
    {
        public static readonly List<GadgetDef> All = new List<GadgetDef>
        {
            new GadgetDef { Name = "Slow-Mo", Description = "Dots move in slow motion", Color = Neon.Aqua, Icon = ShapeType.Circle,
                Apply = (w, p) => w.SlowTimer = 5f },

            new GadgetDef { Name = "Freeze", Description = "Dots freeze in place", Color = new Color32(180, 220, 255, 255), Icon = ShapeType.Diamond,
                Apply = (w, p) => w.FreezeTimer = 3.5f },

            new GadgetDef { Name = "Shield", Description = "Invincible, destroys dots on touch", Color = Neon.Green, Icon = ShapeType.Square,
                Apply = (w, p) => p.GiveShield(6f) },

            new GadgetDef { Name = "Shockwave", Description = "Blasts away nearby dots", Color = Neon.Orange, Icon = ShapeType.Triangle,
                Apply = (w, p) => w.Shockwave(p.Pos, Config.ShockwaveRadius) },

            new GadgetDef { Name = "Nuke", Description = "Destroys every dot", Color = Neon.Pink, Icon = ShapeType.Circle, Weight = 0.5f,
                Apply = (w, p) => w.KillAllEnemies() },

            // ---- inspired by the original's weapons, but with their own look and feel
            new GadgetDef { Name = "Seekers", Description = "Homing darts hunt down the dots", Color = Neon.Lime, Icon = ShapeType.Arrow,
                Apply = (w, p) => w.LaunchSeekers(p.Pos, 6) },

            new GadgetDef { Name = "Lance", Description = "Fires three piercing beams ahead", Color = new Color32(255, 225, 100, 255), Icon = ShapeType.Dart,
                Apply = (w, p) => w.FireLance(p.Pos, p.Facing) },

            new GadgetDef { Name = "Storm", Description = "Lightning leaps between nearby dots", Color = new Color32(150, 130, 255, 255), Icon = ShapeType.Kite,
                Apply = (w, p) => p.GiveStorm(5f) },

            new GadgetDef { Name = "Turret", Description = "Drops an auto-firing turret", Color = Neon.Purple, Icon = ShapeType.Square,
                Apply = (w, p) => w.PlaceTurret(p.Pos) },

            new GadgetDef { Name = "Decoy", Description = "A hologram lures the dots away", Color = new Color32(120, 255, 200, 255), Icon = ShapeType.Ship,
                Apply = (w, p) => w.PlaceDecoy(p.Pos, p.Index) },
        };

        public static GadgetDef PickRandom(System.Random rng)
        {
            float total = 0f;
            foreach (var g in All) total += g.Weight;
            float r = (float)rng.NextDouble() * total;
            foreach (var g in All) { r -= g.Weight; if (r <= 0f) return g; }
            return All[0];
        }
    }

    /// <summary>A collectible lying on the map.</summary>
    public class Pickup : MonoBehaviour
    {
        public const float Radius = 0.4f;
        public GadgetDef Def;
        public float Life;
        public Vector2 Pos { get { return transform.position; } }
        ShapeView view;

        public static Pickup Spawn(Transform parent, GadgetDef def, Vector2 pos)
        {
            var go = new GameObject("Pickup_" + def.Name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var p = go.AddComponent<Pickup>();
            p.Def = def; p.Life = Config.GadgetLifetime;
            p.view = ShapeView.Create(go.transform, def.Icon, Radius,
                new Color(def.Color.r, def.Color.g, def.Color.b, 0.25f), def.Color, 0.6f, 7);
            return p;
        }

        /// <returns>false when expired</returns>
        public bool Tick(float dt)
        {
            Life -= dt;
            view.transform.localScale = Vector3.one * (1f + 0.12f * Mathf.Sin(Time.time * 8f));
            view.transform.Rotate(0, 0, 60f * dt);
            view.Alpha = Life < 2f ? (Mathf.Sin(Time.time * 20f) > 0 ? 1f : 0.3f) : 1f;
            return Life > 0f;
        }
    }
}
