using System;
using System.Collections.Generic;
using UnityEngine;

namespace TiltRush
{
    /// <summary>
    /// The tutorial: BLIP (a friendly dot) explains the game step by step, with a few jokes.
    /// You cannot die here - getting hit gives you a short shield and a comment.
    /// The script is one coroutine-like method (Script) - edit the lines and steps there.
    /// </summary>
    public class TutorialDirector
    {
        class Wait { public float Time; public Func<bool> Until; }

        readonly GameWorld w;
        IEnumerator<Wait> script;
        Wait current;
        float cooldown, stepDt;
        int hits;

        static readonly string[] HitLines =
        {
            "Ouch! Good thing tutorials come with free shields.",
            "That one hugged you. We TALKED about this.",
            "Don't worry, nobody saw that. Except me. And I'm telling everyone.",
            "Pro tip: the spiky side is the bad side. All sides are spiky.",
            "Shield on the house. Next time it costs extra.",
        };

        public TutorialDirector(GameWorld world) { w = world; script = Script(); }

        public void Tick(float dt)
        {
            stepDt = dt;
            if (cooldown > 0f) cooldown -= dt;
            if (current != null)
            {
                if (current.Until != null) { if (!current.Until()) return; }
                else { current.Time -= dt; if (current.Time > 0f) return; }
            }
            current = script.MoveNext() ? script.Current : null;   // null = continue next step
        }

        /// <summary>Called instead of dying.</summary>
        public void OnHit(PlayerController p)
        {
            p.GiveShield(2f);
            if (cooldown > 0f) return;
            cooldown = 3f;
            w.Say(HitLines[hits++ % HitLines.Length]);
        }

        static Wait Seconds(float s) { return new Wait { Time = s }; }
        static Wait Until(Func<bool> f) { return new Wait { Until = f }; }

        Vector2 Arena(float x, float y) { return new Vector2(x * Config.ArenaSize.x, y * Config.ArenaSize.y); }

        IEnumerator<Wait> Script()
        {
            // 1) hello + moving
            w.Say("Hi! I'm BLIP. I'm a dot - but one of the NICE ones. Promise.");
            yield return Seconds(3.5f);
            w.Say(Platform.MoveHint + " Fly through the glowing rings!");

            string[] praise = { "Nice! You moved! The first step of a legend.", "Again! Okay, show-off.", "Three for three. You're a natural!" };
            Vector2[] spots = { Arena(-0.3f, 0.25f), Arena(0.32f, -0.22f), Arena(0.05f, 0.3f) };
            for (int i = 0; i < spots.Length; i++)
            {
                var ring = TutorialTarget.Spawn(w.transform, spots[i]);
                yield return Until(() => ring.Reached(w));
                ring.Pop();
                w.Say(praise[i]);
                yield return Seconds(i < spots.Length - 1 ? 0.6f : 2.5f);
            }

            // 2) enemies
            w.Say("Now meet my cousins. Spiky, grumpy, and they REALLY want a hug.");
            yield return Seconds(3f);
            for (int i = 0; i < 6; i++)
                w.TutorialEnemy(EnemyType.Normal, Arena(i % 2 == 0 ? -0.45f : 0.45f, -0.4f + i * 0.16f), 0.75f);
            float survive = 10f;
            w.Say("Don't let them touch you! Survive for 10 seconds.");
            while (survive > 0f)
            {
                survive -= stepDt;
                w.Countdown(survive);
                yield return null;
            }
            w.Countdown(-1f);
            w.ClearEnemies();
            w.Say("You survived! My cousins are disappointed. That's a good sign.");
            yield return Seconds(3f);

            // 3) gadgets
            for (int i = 0; i < 8; i++) w.TutorialEnemy(EnemyType.Normal, Arena(-0.45f + i * 0.13f, i % 2 == 0 ? 0.42f : -0.42f), 0.6f);
            var shock = GadgetLibrary.All.Find(g => g.Name == "Shockwave");
            Vector2 ppos = w.Players[0].Pos;
            w.TutorialPickup(shock, ppos + (ppos.x > 0 ? Vector2.left : Vector2.right) * 3f);
            int before = w.PickupsCollected;
            w.Say("Uh-oh, here they come again. Grab that shiny thing - it's a GADGET!");
            yield return Until(() => w.PickupsCollected > before);
            w.Say("BOOM! Gadgets make problems disappear. Mostly. There are 14 of them.");
            yield return Seconds(1.5f);
            w.ClearEnemies();
            yield return Seconds(2f);

            // 4) wall
            w.Say("Careful: a RED WALL. Walls don't hug, they squish. Go around the ends!");
            yield return Seconds(1.5f);
            w.TutorialWall();
            yield return Until(() => w.HazardCount == 0);
            w.Say("Squish avoided. Impressive!");
            yield return Seconds(2.5f);

            // 5) sword
            w.Say("See that red lane? A SWORD is coming through it. Get OUT of the lane!");
            w.TutorialSword();
            yield return Until(() => w.HazardCount == 0);
            w.Say("Sword dodged! You're basically a ninja now.");
            yield return Seconds(2.5f);

            // 6) bye
            w.Say("That's all! The rest you'll learn the hard way. Good luck... you'll need it! :)");
            yield return Seconds(3.5f);
            w.FinishTutorial();
        }
    }

    /// <summary>Glowing ring you have to fly through in the tutorial.</summary>
    public class TutorialTarget : MonoBehaviour
    {
        ShapeView ring, core;
        public Vector2 Pos { get { return transform.position; } }

        public static TutorialTarget Spawn(Transform parent, Vector2 pos)
        {
            var go = new GameObject("Target");
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var t = go.AddComponent<TutorialTarget>();
            t.ring = ShapeView.Create(go.transform, ShapeType.Circle, 0.6f, new Color(0f, 1f, 1f, 0.12f), Neon.Aqua, 0.8f, 3);
            t.core = ShapeView.Create(go.transform, ShapeType.Circle, 0.15f, Color.white, Neon.Aqua, 0.6f, 4);
            Fx.Burst(pos, Neon.Aqua, 0.2f, 1.2f, 0.4f);
            return t;
        }

        public bool Reached(GameWorld w)
        {
            foreach (var p in w.Players) if (p.Alive && (p.Pos - Pos).sqrMagnitude < 0.7f * 0.7f) return true;
            return false;
        }

        public void Pop()
        {
            Fx.Burst(Pos, Neon.Aqua, 0.4f, 2f, 0.4f);
            Fx.Sparks(Pos, Neon.Aqua, 18, 7f, 0.5f);
            App.I.Audio.Play("pickup");
            Destroy(gameObject);
        }

        void Update()
        {
            float s = 1f + 0.12f * Mathf.Sin(Time.time * 5f);
            ring.transform.localScale = Vector3.one * s;
        }
    }
}
