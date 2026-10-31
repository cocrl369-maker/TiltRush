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
        public GadgetIcon Icon;   // pictogram on the pickup (SpriteFactory.Gadget)
        public float Weight = 1f;   // spawn probability weight
        public bool InNormal = true;      // appears in the normal modes
        public bool InAsteroids = true;   // appears in the asteroid field
        public Action<GameWorld, PlayerController> Apply;
    }

    public static class GadgetLibrary
    {
        public static readonly List<GadgetDef> All = new List<GadgetDef>
        {
            new GadgetDef { Name = "Slow-Mo", Description = "Dots move in slow motion", Color = Neon.Aqua, Icon = GadgetIcon.Clock, InAsteroids = false,
                Apply = (w, p) => w.SlowTimer = 5f },

            new GadgetDef { Name = "Freeze", Description = "Freezes nearby dots into ice - fly into them to smash them!", Color = new Color32(180, 220, 255, 255), Icon = GadgetIcon.Snowflake, InAsteroids = false,
                Apply = (w, p) => w.FreezeArea(p.Pos, Config.FreezeRadius) },

            new GadgetDef { Name = "Shield", Description = "Invincible, destroys dots on touch", Color = Neon.Green, Icon = GadgetIcon.Shield,
                Apply = (w, p) => p.GiveShield(6f) },

            new GadgetDef { Name = "Shockwave", Description = "Blasts away nearby dots", Color = Neon.Orange, Icon = GadgetIcon.Shockwave,
                Apply = (w, p) => w.Shockwave(p.Pos, Config.ShockwaveRadius) },

            new GadgetDef { Name = "Nuke", Description = "Destroys every dot", Color = Neon.Pink, Icon = GadgetIcon.Nuke, Weight = 0.5f,
                Apply = (w, p) => w.KillAllEnemies() },

            // ---- inspired by the original's weapons, but with their own look and feel
            new GadgetDef { Name = "Seekers", Description = "Homing darts hunt down the dots", Color = Neon.Lime, Icon = GadgetIcon.Crosshair,
                Apply = (w, p) => w.LaunchSeekers(p.Pos, 6) },

            new GadgetDef { Name = "Lance", Description = "Fires three piercing beams ahead", Color = new Color32(255, 225, 100, 255), Icon = GadgetIcon.Lance,
                Apply = (w, p) => w.FireLance(p.Pos, p.Facing) },

            new GadgetDef { Name = "Storm", Description = "Lightning leaps between nearby dots", Color = new Color32(150, 130, 255, 255), Icon = GadgetIcon.Bolt,
                Apply = (w, p) => p.GiveStorm(5f) },

            new GadgetDef { Name = "Turret", Description = "Drops an auto-firing turret", Color = Neon.Purple, Icon = GadgetIcon.Turret, InAsteroids = false,
                Apply = (w, p) => w.PlaceTurret(p.Pos) },

            new GadgetDef { Name = "Decoy", Description = "A hologram lures the dots away", Color = new Color32(120, 255, 200, 255), Icon = GadgetIcon.Ghost, InAsteroids = false,
                Apply = (w, p) => w.PlaceDecoy(p.Pos, p.Index) },

            new GadgetDef { Name = "Shrink", Description = "You become tiny - slip through the gaps", Color = new Color32(255, 240, 120, 255), Icon = GadgetIcon.Shrink,
                Apply = (w, p) => p.GiveShrink(7f) },

            new GadgetDef { Name = "Boost", Description = "Fly much faster for a few seconds", Color = new Color32(255, 160, 40, 255), Icon = GadgetIcon.Boost,
                Apply = (w, p) => p.GiveBoost(6f) },

            new GadgetDef { Name = "Black Hole", Description = "Sucks in nearby dots and swallows them", Color = new Color32(170, 90, 255, 255), Icon = GadgetIcon.Vortex, Weight = 0.7f,
                Apply = (w, p) => w.PlaceBlackHole(p.Pos) },

            new GadgetDef { Name = "Orbit", Description = "Three orbs circle around you and smash dots", Color = new Color32(90, 200, 255, 255), Icon = GadgetIcon.Orbit,
                Apply = (w, p) => p.GiveOrbit(8f) },

            // asteroid field only
            new GadgetDef { Name = "Spread Shot", Description = "Asteroid Field: your cannon fires five shots at once", Color = new Color32(255, 90, 90, 255),
                Icon = GadgetIcon.Spread, Weight = 1.6f, InNormal = false,
                Apply = (w, p) => p.GiveSpread(8f) },
        };

        public static GadgetDef PickRandom(System.Random rng, bool asteroids = false)
        {
            float total = 0f;
            foreach (var g in All) if (Allowed(g, asteroids)) total += g.Weight;
            float r = (float)rng.NextDouble() * total;
            foreach (var g in All) { if (!Allowed(g, asteroids)) continue; r -= g.Weight; if (r <= 0f) return g; }
            return All[0];
        }

        static bool Allowed(GadgetDef g, bool asteroids) { return asteroids ? g.InAsteroids : g.InNormal; }
    }

    /// <summary>A collectible lying on the map.</summary>
    public class Pickup : MonoBehaviour
    {
        public const float Radius = 0.4f;
        public GadgetDef Def;
        public float Life;
        public Vector2 Pos { get { return transform.position; } }
        ShapeView view;

        Transform token;
        SpriteRenderer icon;

        /// <summary>Round token in the gadget colour with a white pictogram (clock, snowflake, shield ...).</summary>
        public static Pickup Spawn(Transform parent, GadgetDef def, Vector2 pos)
        {
            var go = new GameObject("Pickup_" + def.Name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var p = go.AddComponent<Pickup>();
            p.Def = def; p.Life = Config.GadgetLifetime;
            p.token = new GameObject("Token").transform;
            p.token.SetParent(go.transform, false);

            Color dark = new Color(def.Color.r * 0.35f, def.Color.g * 0.35f, def.Color.b * 0.35f, 1f);
            p.view = ShapeView.Create(p.token, ShapeType.Circle, Radius, dark, def.Color, 0.7f, 7);
            p.view.AddBacking(new Color(0f, 0f, 0f, 0.75f), 1.12f);    // dark ring: visible on every background

            var ig = new GameObject("Icon");
            ig.transform.SetParent(p.token, false);
            ig.transform.localScale = Vector3.one * Radius * 1.3f;
            p.icon = ig.AddComponent<SpriteRenderer>();
            p.icon.sprite = SpriteFactory.Gadget(def.Icon);
            p.icon.sortingOrder = 9;
            return p;
        }

        /// <returns>false when expired</returns>
        public bool Tick(float dt)
        {
            Life -= dt;
            // gentle bounce and wobble (the pictogram stays upright and readable)
            token.localScale = Vector3.one * (1f + 0.1f * Mathf.Sin(Time.time * 6f));
            token.localRotation = Quaternion.Euler(0, 0, 10f * Mathf.Sin(Time.time * 3f));
            float a = Life < 2f ? (Mathf.Sin(Time.time * 20f) > 0 ? 1f : 0.3f) : 1f;   // blinks before it disappears
            view.Alpha = a;
            icon.color = new Color(1f, 1f, 1f, a);
            return Life > 0f;
        }
    }
}
