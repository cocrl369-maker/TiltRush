using UnityEngine;
using UnityEngine.UI;

namespace TiltRush
{
    /// <summary>
    /// Tilt setup: choose how the "neutral" pose (where the ship stands still) is determined -
    /// automatically, a preset for a typical way of holding the phone, or calibrated right now.
    /// A live preview dot shows what the game receives.
    /// </summary>
    public class TiltSetupScreen : UIScreen
    {
        Button[] presetBtns;
        Button calibBtn;
        Text status, msg;
        RectTransform dot;
        IMoveInput preview;
        protected override bool HasVignette { get { return true; } }

        static bool IsPhone { get { return (Application.isMobilePlatform && !Application.isEditor && SystemInfo.supportsAccelerometer) || Platform.RemoteConnected; } }

        protected override void Build()
        {
            var root = (RectTransform)transform;
            new UIStack(root, 30).Label("TILT SETUP", 80, Color.white, 100);
            UIKit.Panel(transform, -520, 150, 760, 780);
            UIKit.Panel(transform, 480, 150, 760, 780);

            // left: poses
            var left = new UIStack(root, 170, -520, 700);
            left.Spacing = 12;
            left.Label("Hold your phone the way you like to play, then pick a pose - or press CALIBRATE NOW. In that pose the ship stands still.",
                       28, Theme.TextDim, 120, TextAnchor.UpperLeft);
            presetBtns = new Button[TiltInput.PresetNames.Length];
            for (int i = 0; i < presetBtns.Length; i++)
            {
                int idx = i;
                presetBtns[i] = left.Button(TiltInput.PresetNames[i] + "\n<size=22>" + TiltInput.PresetHints[i] + "</size>",
                                            Neon.Gray, 700, 84, () => SetPreset(idx), 34);
            }
            calibBtn = left.Button("CALIBRATE NOW", Neon.Orange, 700, 100, Calibrate, 42);

            // right: live preview + sensitivity
            var right = new UIStack(root, 170, 480, 700);
            right.Spacing = 14;
            var box = UIKit.Rect("PreviewBox", transform);
            right.Place(box, 360, 360);
            var bg = box.gameObject.AddComponent<Image>();
            bg.sprite = SpriteFactory.RoundedFill(); bg.type = Image.Type.Sliced;
            bg.color = new Color(0.1f, 0.11f, 0.16f, 1f); bg.raycastTarget = false;
            Line(box, new Vector2(300, 2));
            Line(box, new Vector2(2, 300));

            dot = UIKit.Rect("Dot", box);
            dot.anchorMin = dot.anchorMax = dot.pivot = new Vector2(0.5f, 0.5f);
            dot.sizeDelta = new Vector2(44, 44);
            var dotImg = dot.gameObject.AddComponent<Image>();
            dotImg.sprite = SpriteFactory.Fill(ShapeType.Circle); dotImg.color = Config.Palette[0]; dotImg.raycastTarget = false;

            status = right.Label("", 34, Color.white, 50);
            msg = right.Label("", 28, Theme.TextDim, 90, TextAnchor.UpperCenter);
            right.Slider("SENSITIVITY", 1f, 6f, SaveManager.Data.tiltSensitivity, v => SaveManager.Data.tiltSensitivity = v);

            MenuUtil.AddBack(transform, () => { SaveManager.Save(); UI.Show<OptionsScreen>(); });
        }

        static void Line(RectTransform parent, Vector2 size)
        {
            var rt = UIKit.Rect("Cross", parent);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            var img = rt.gameObject.AddComponent<Image>();
            img.color = new Color(1, 1, 1, 0.18f); img.raycastTarget = false;
        }

        void SetPreset(int index)
        {
            SaveManager.Data.tiltPreset = index;
            preview.Calibrate();
            msg.text = "";
            Refresh();
        }

        void Calibrate()
        {
            if (!IsPhone) { msg.text = "Calibrating needs a real phone (accelerometer)."; return; }
            var d = SaveManager.Data;
            d.customNeutral = Input.acceleration;
            d.customOrientation = (int)TiltInput.Landscape();
            d.hasCustomTilt = true;
            d.tiltPreset = 5;
            preview.Calibrate();
            msg.text = "Calibrated! The ship stands still in this pose.";
            Refresh();
        }

        void Refresh()
        {
            int p = SaveManager.Data.tiltPreset;
            for (int i = 0; i < presetBtns.Length; i++) UIKit.SetButtonColor(presetBtns[i], i == p ? Neon.Blue : Neon.Gray);
            UIKit.SetButtonColor(calibBtn, p == 5 ? Neon.Green : Neon.Orange);
            status.text = "ACTIVE: " + (p == 5 ? "CUSTOM (calibrated)" : TiltInput.PresetNames[p]);
        }

        public override void OnShow()
        {
            preview = InputFactory.Create(0, 1);    // tilt on a phone, keyboard on a PC
            preview.Calibrate();
            msg.text = IsPhone ? "" : "On a PC the dot follows WASD / arrow keys. Tilt setup matters on your phone.";
            Refresh();
        }

        void Update()
        {
            if (preview == null) return;
            dot.anchoredPosition = Vector2.Lerp(dot.anchoredPosition, preview.Read() * 150f, 0.4f);
        }
    }
}
