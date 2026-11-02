using UnityEngine;
using UnityEngine.UI;

namespace TiltRush
{
    /// <summary>
    /// CONTROLS: tilt setup, sensitivity, smoothing, axes, invert. Reachable from the main menu, the mode select
    /// and the pause menu - BACK returns to where you came from.
    /// </summary>
    public class ControlsScreen : UIScreen
    {
        System.Action back;
        readonly System.Collections.Generic.List<System.Action> refreshers = new System.Collections.Generic.List<System.Action>();
        protected override bool HasVignette { get { return true; } }
        public override string MusicContext { get { return GameWorld.Current != null ? null : "Menu"; } }

        /// <summary>Opens the screen; 'backTo' runs when BACK is pressed.</summary>
        public static void Open(System.Action backTo)
        {
            var s = App.I.UI.Get<ControlsScreen>();
            s.back = backTo;
            App.I.UI.Show<ControlsScreen>();
        }

        protected override void Build()
        {
            var title = UIKit.Title(transform, "CONTROLS", 96);
            UIKit.Anchor(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -25), new Vector2(1200, 120));
            UIKit.Panel(transform, 0, 160, 1720, 760);

            var l = new UIStack((RectTransform)transform, 200, -420, 760) { Spacing = 16f };
            l.Button("TILT SETUP", Neon.Blue, 760, 110, () => UI.Show<TiltSetupScreen>(), 44);
            refreshers.Add(l.SliderRow("SENSITIVITY", 1f, 6f, () => SaveManager.Data.tiltSensitivity, v => SaveManager.Data.tiltSensitivity = v));
            refreshers.Add(l.Choice("SMOOTHING", new[] { "DIRECT", "NORMAL", "SMOOTH" }, () => SaveManager.Data.tiltSmoothing, v => SaveManager.Data.tiltSmoothing = v));
            refreshers.Add(l.Choice("TILT AXES", new[] { "STANDARD", "SWAPPED" }, () => SaveManager.Data.tiltAxes, v => SaveManager.Data.tiltAxes = v));

            var r = new UIStack((RectTransform)transform, 200, 420, 760) { Spacing = 16f };
            refreshers.Add(r.Toggle("INVERT LEFT/RIGHT", () => SaveManager.Data.invertX, v => SaveManager.Data.invertX = v));
            refreshers.Add(r.Toggle("INVERT UP/DOWN", () => SaveManager.Data.invertY, v => SaveManager.Data.invertY = v));
            r.Space(10);
            var hint = r.Label("Tilt right but the ship goes up/down? Set TILT AXES to SWAPPED.\n\n" + Platform.CoopHint,
                               30, Theme.TextDim, 330, TextAnchor.UpperLeft);
            UIKit.ShrinkToFit(hint);

            MenuUtil.AddBack(transform, () =>
            {
                SaveManager.Save();
                if (back != null) back();
                else if (GameWorld.Current != null) App.I.BackToPause();
                else UI.Show<MainMenuScreen>();
            });
        }

        public override void OnShow() { foreach (var f in refreshers) f(); }
    }
}
