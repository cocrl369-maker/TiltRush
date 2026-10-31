using UnityEngine;

namespace TiltRush
{
    /// <summary>Every control method returns a vector with length 0..1 in SCREEN space. Add new schemes by implementing this.</summary>
    public interface IMoveInput
    {
        Vector2 Read();
        void Calibrate();   // called when a round starts
    }

    /// <summary>
    /// Phone tilt (landscape aware). The "neutral" pose - where the ship stands still - comes from the
    /// tilt setup: Auto (pose when the countdown ends), a preset for a typical way of holding the phone,
    /// or a custom pose saved with CALIBRATE NOW.
    /// </summary>
    public class TiltInput : IMoveInput
    {
        public static readonly string[] PresetNames = { "AUTO", "FLAT", "RELAXED", "UPRIGHT", "LYING DOWN" };
        public static readonly string[] PresetHints = { "pose at round start", "phone held flat", "sofa / in hand, ~35 deg", "desk / upright, ~60 deg", "lying in bed, screen down" };
        static readonly float[] PresetPitch = { 0f, 0f, 35f, 60f, 160f };   // how far the top edge is raised towards you

        Vector3 neutral;
        const float DeadZone = 0.03f;

        public TiltInput() { Calibrate(); }
        public void Calibrate() { neutral = ResolveNeutral(); }

        static Vector3 ResolveNeutral()
        {
            var d = SaveManager.Data;
            if (d.tiltPreset == 0) return Input.acceleration;
            if (d.tiltPreset == 5)
            {
                if (!d.hasCustomTilt) return Input.acceleration;
                Vector3 c = d.customNeutral;
                if ((int)Screen.orientation != d.customOrientation) c = new Vector3(-c.x, -c.y, c.z);   // phone was turned around 180 deg
                return c;
            }
            float r = PresetPitch[Mathf.Clamp(d.tiltPreset, 1, 4)] * Mathf.Deg2Rad;
            float k = Screen.orientation == ScreenOrientation.LandscapeRight ? 1f : -1f;
            return new Vector3(k * Mathf.Sin(r), 0f, -Mathf.Cos(r));
        }

        public Vector2 Read()
        {
            Vector3 a = Input.acceleration - neutral;   // device axes (portrait based)
            Vector2 v;
            switch (Screen.orientation)
            {
                case ScreenOrientation.LandscapeLeft:  v = new Vector2(-a.y, a.x); break;
                case ScreenOrientation.LandscapeRight: v = new Vector2(a.y, -a.x); break;
                default:                               v = new Vector2(a.x, a.y); break;
            }
            v *= SaveManager.Data.tiltSensitivity;
            if (v.magnitude < DeadZone) return Vector2.zero;
            return Vector2.ClampMagnitude(v, 1f);
        }
    }

    public enum KeyScheme { Both, WASD, Arrows }

    public class KeyboardInput : IMoveInput
    {
        readonly KeyScheme scheme;
        public KeyboardInput(KeyScheme s) { scheme = s; }
        public void Calibrate() { }

        public Vector2 Read()
        {
            float x = 0, y = 0;
            if (scheme != KeyScheme.Arrows)
            {
                if (Input.GetKey(KeyCode.A)) x -= 1;
                if (Input.GetKey(KeyCode.D)) x += 1;
                if (Input.GetKey(KeyCode.W)) y += 1;
                if (Input.GetKey(KeyCode.S)) y -= 1;
            }
            if (scheme != KeyScheme.WASD)
            {
                if (Input.GetKey(KeyCode.LeftArrow)) x -= 1;
                if (Input.GetKey(KeyCode.RightArrow)) x += 1;
                if (Input.GetKey(KeyCode.UpArrow)) y += 1;
                if (Input.GetKey(KeyCode.DownArrow)) y -= 1;
            }
            return Vector2.ClampMagnitude(new Vector2(x, y), 1f);
        }
    }

    /// <summary>Floating virtual stick: put a finger anywhere and drag.</summary>
    public class TouchStickInput : IMoveInput
    {
        int finger = -1;
        Vector2 origin;
        public void Calibrate() { finger = -1; }

        public Vector2 Read()
        {
            float radius = Screen.height * 0.12f;
            foreach (Touch t in Input.touches)
            {
                if (finger == -1 && t.phase == TouchPhase.Began) { finger = t.fingerId; origin = t.position; }
                if (t.fingerId != finger) continue;
                if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled) { finger = -1; return Vector2.zero; }
                return Vector2.ClampMagnitude((t.position - origin) / radius, 1f);
            }
            return Vector2.zero;
        }
    }

    /// <summary>Uses the first input that is currently active (e.g. keyboard OR tilt).</summary>
    public class CombinedInput : IMoveInput
    {
        readonly IMoveInput[] parts;
        public CombinedInput(params IMoveInput[] parts) { this.parts = parts; }
        public void Calibrate() { foreach (var p in parts) p.Calibrate(); }

        public Vector2 Read()
        {
            foreach (var p in parts)
            {
                Vector2 v = p.Read();
                if (v.sqrMagnitude > 0.0001f) return v;
            }
            return Vector2.zero;
        }
    }

    public static class InputFactory
    {
        public static IMoveInput Create(int index, int playerCount)
        {
            // Real phone / tablet: steer ONLY by tilting. (Co-op on one phone: player 2 gets a touch stick,
            // because two people cannot both tilt the same phone.)
            bool phone = Application.isMobilePlatform && !Application.isEditor;
            if (phone)
            {
                if (playerCount > 1 && index == 1) return new TouchStickInput();
                return new TiltInput();
            }

            // PC / Unity editor (also Device Simulator): keyboard
            if (playerCount > 1) return new KeyboardInput(index == 0 ? KeyScheme.WASD : KeyScheme.Arrows);
            return new KeyboardInput(KeyScheme.Both);
        }
    }
}
