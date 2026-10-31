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

        Vector3 neutral, filtered;
        int lastFrame = -1;
        const float DeadZone = 0.04f;
        // low-pass filter against sensor jitter (higher = more direct, lower = smoother) - Options > Controls > Smoothing
        static readonly float[] SmoothingLevels = { 45f, 20f, 9f };

        public TiltInput() { Calibrate(); }

        public void Calibrate()
        {
            filtered = Input.acceleration;   // restart the filter from the current pose
            lastFrame = Time.frameCount;
            neutral = ResolveNeutral(filtered);
        }

        /// <summary>
        /// Current landscape orientation. Screen.orientation can report AutoRotation / Portrait on some Unity versions
        /// and devices, so the device orientation and the screen shape are used as fallbacks.
        /// </summary>
        public static ScreenOrientation Landscape()
        {
            var o = Screen.orientation;
            if (o == ScreenOrientation.LandscapeLeft || o == ScreenOrientation.LandscapeRight) { last = o; return o; }
            var d = Input.deviceOrientation;
            if (d == DeviceOrientation.LandscapeLeft) last = ScreenOrientation.LandscapeLeft;
            else if (d == DeviceOrientation.LandscapeRight) last = ScreenOrientation.LandscapeRight;
            return last;
        }
        static ScreenOrientation last = ScreenOrientation.LandscapeLeft;

        static Vector3 ResolveNeutral(Vector3 current)
        {
            var d = SaveManager.Data;
            if (d.tiltPreset == 0) return current;
            if (d.tiltPreset == 5)
            {
                if (!d.hasCustomTilt) return current;
                Vector3 c = d.customNeutral;
                // only the "swapped" (device axes) mode has to mirror a pose calibrated with the phone turned around
                if (d.tiltAxes == 1 && (int)Landscape() != d.customOrientation) c = new Vector3(-c.x, -c.y, c.z);
                return c;
            }
            float r = PresetPitch[Mathf.Clamp(d.tiltPreset, 1, 4)] * Mathf.Deg2Rad;
            if (d.tiltAxes == 0) return new Vector3(0f, -Mathf.Sin(r), -Mathf.Cos(r));   // top edge raised towards you
            float k = Landscape() == ScreenOrientation.LandscapeRight ? 1f : -1f;
            return new Vector3(k * Mathf.Sin(r), 0f, -Mathf.Cos(r));
        }

        public Vector2 Read()
        {
            // the filter advances once per rendered frame (Read can be called several times per frame)
            if (Time.frameCount != lastFrame)
            {
                lastFrame = Time.frameCount;
                float smoothing = SmoothingLevels[Mathf.Clamp(SaveManager.Data.tiltSmoothing, 0, 2)];
                float k = 1f - Mathf.Exp(-smoothing * Mathf.Min(Time.unscaledDeltaTime, 0.1f));
                filtered = Vector3.Lerp(filtered, Input.acceleration, k);
            }

            Vector3 a = filtered - neutral;
            // STANDARD: Unity already reports the acceleration in screen axes (x = right, y = up) for the current
            // landscape orientation. SWAPPED (Options > Controls): for devices that report raw portrait axes.
            Vector2 v = SaveManager.Data.tiltAxes == 0
                ? new Vector2(a.x, a.y)
                : (Landscape() == ScreenOrientation.LandscapeRight ? new Vector2(a.y, -a.x) : new Vector2(-a.y, a.x));
            v *= SaveManager.Data.tiltSensitivity;
            if (SaveManager.Data.invertX) v.x = -v.x;
            if (SaveManager.Data.invertY) v.y = -v.y;

            // soft dead zone: no jump from 0 to full speed at the edge of the zone
            float m = v.magnitude;
            if (m < DeadZone) return Vector2.zero;
            return v / m * Mathf.Min(1f, (m - DeadZone) / (1f - DeadZone));
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

    /// <summary>
    /// Floating thumb stick: put a finger down in your zone and drag. Zone 0 = left half of the screen,
    /// 1 = right half, -1 = anywhere. Used for co-op on one phone (phone lies flat on the table).
    /// The HUD draws all active sticks (TouchStickInput.All).
    /// </summary>
    public class TouchStickInput : IMoveInput
    {
        public static readonly System.Collections.Generic.List<TouchStickInput> All = new System.Collections.Generic.List<TouchStickInput>();

        public readonly int Zone;
        public bool Held { get; private set; }
        public Vector2 Origin { get; private set; }     // screen pixels
        public Vector2 Knob { get; private set; }       // screen pixels (clamped to the stick radius)
        public static float RadiusPx { get { return Screen.height * 0.11f; } }

        int finger = -1;

        public TouchStickInput(int zone) { Zone = zone; All.Add(this); }
        public void Calibrate() { finger = -1; Held = false; }

        bool InZone(Vector2 p)
        {
            if (Zone == 0) return p.x < Screen.width * 0.5f;
            if (Zone == 1) return p.x >= Screen.width * 0.5f;
            return true;
        }

        static bool OverButton(int fingerId)
        {
            var es = UnityEngine.EventSystems.EventSystem.current;
            return es != null && es.IsPointerOverGameObject(fingerId);
        }

        public Vector2 Read()
        {
            float radius = RadiusPx;
            bool found = false;
            foreach (Touch t in Input.touches)
            {
                if (finger == -1 && t.phase == TouchPhase.Began && InZone(t.position) && !OverButton(t.fingerId) && !Claimed(t.fingerId))
                { finger = t.fingerId; Origin = t.position; }
                if (t.fingerId != finger) continue;
                found = true;
                if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled) break;
                Vector2 off = Vector2.ClampMagnitude(t.position - Origin, radius);
                Knob = Origin + off;
                Held = true;
                return off / radius;
            }
            if (!found || finger != -1) { finger = -1; Held = false; }
            return Vector2.zero;
        }

        bool Claimed(int fingerId)
        {
            foreach (var s in All) if (s != this && s.finger == fingerId) return true;
            return false;
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
            // Real phone / tablet: solo = tilt only. Co-op = the phone lies flat between the players and each one
            // gets a thumb stick on their half of the screen (two people cannot tilt the same phone).
            bool phone = Application.isMobilePlatform && !Application.isEditor;
            if (phone)
            {
                if (playerCount > 1) return new TouchStickInput(index);
                return new TiltInput();
            }

            // Editor + Unity Remote app: tilt / touch from the phone, keyboard still works
            if (Platform.RemoteConnected)
            {
                if (playerCount > 1) return new CombinedInput(new TouchStickInput(index), new KeyboardInput(index == 0 ? KeyScheme.WASD : KeyScheme.Arrows));
                return new CombinedInput(new TiltInput(), new KeyboardInput(KeyScheme.Both));
            }

            // PC / Unity editor (also Device Simulator): keyboard - only for testing
            if (playerCount > 1) return new KeyboardInput(index == 0 ? KeyScheme.WASD : KeyScheme.Arrows);
            return new KeyboardInput(KeyScheme.Both);
        }
    }
}
