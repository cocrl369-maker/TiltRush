using UnityEngine;

// The game reads the phone tilt with the classic Input class. Unity 6 projects use the NEW Input System by default,
// which makes every Input call throw an error. Fix: Edit > Project Settings > Player > Other Settings >
// Active Input Handling = "Both" (Unity restarts once).
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
#error TILT RUSH: set Edit > Project Settings > Player > Other Settings > Active Input Handling to "Both" (see README).
#endif

namespace TiltRush
{
    /// <summary>
    /// Entry point. Creates itself automatically when ANY scene starts (no scene setup required):
    /// camera, background, audio, UI and online service are all built here.
    /// </summary>
    public class App : MonoBehaviour
    {
        public static App I { get; private set; }

        public UIManager UI;
        public AudioManager Audio;
        public IOnlineService Online;
        public SessionController Session;
        Camera cam;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (I == null) new GameObject("App").AddComponent<App>();
        }

        void Awake()
        {
            I = this;
            DontDestroyOnLoad(gameObject);

            Screen.autorotateToPortrait = false;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = true;
            Screen.orientation = ScreenOrientation.AutoRotation;   // landscape only
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Input.multiTouchEnabled = true;

            SaveManager.Load();
            ApplyFrameRate();
            SetupCamera();

            Audio = gameObject.AddComponent<AudioManager>();
            gameObject.AddComponent<NeonBackground>();
            Online = new MockOnlineService();          // <- swap for a real backend later
            Session = new SessionController(this);

            UI = gameObject.AddComponent<UIManager>();
            UI.Build();
            if (SaveManager.Data.tutorialDone) UI.Show<MainMenuScreen>();
            else Session.StartTutorial();          // very first start: straight into the tutorial (can be skipped)
        }

        void SetupCamera()
        {
            cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera");
                go.tag = "MainCamera";
                cam = go.AddComponent<Camera>();
                go.AddComponent<AudioListener>();
            }
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.transform.position = new Vector3(0, 0, -10);
        }

        Vector3 camPos = new Vector3(0, 0, -10);
        float camSize = Config.CameraSize;
        float shakeMag, shakeTime, shakeDur = 1f;

        /// <summary>Short camera shake (nuke, shockwave, death). Can be switched off in the options.</summary>
        public void Shake(float magnitude, float duration)
        {
            if (!SaveManager.Data.screenShake) return;
            float current = shakeMag * Mathf.Clamp01(shakeTime / shakeDur);
            if (magnitude < current) return;                      // don't let a small shake cancel a big one
            shakeMag = magnitude; shakeDur = Mathf.Max(0.01f, duration); shakeTime = duration;
        }

        // Pausing when the app goes to the background (phone call, home button, notification shade)
        void OnApplicationPause(bool paused) { if (paused) Pause(); }
        void OnApplicationFocus(bool focused) { if (!focused && !Application.isEditor) Pause(); }

        void LateUpdate()
        {
            cam.orthographicSize = camSize;
            if (GameWorld.Current == null) FitArena(false);    // the arena size only changes between rounds
            Vector3 p = camPos;
            if (shakeTime > 0f)
            {
                shakeTime -= Time.unscaledDeltaTime;
                Vector2 o = Random.insideUnitCircle * shakeMag * Mathf.Clamp01(shakeTime / shakeDur);
                p += new Vector3(o.x, o.y, 0f);
            }
            cam.transform.position = p;
        }

        /// <summary>
        /// Makes the arena as large as possible: it fills the whole screen except the safe-area insets
        /// (notch, rounded corners). The camera is shifted so the arena centre is always world (0,0).
        /// </summary>
        /// <param name="fixedSize">
        /// true (online PvP): the arena gets the same size on every phone (Config.OnlineArenaSize) and the camera zooms
        /// so it fits - both players really play the identical map. false: the arena fills the whole screen.
        /// </param>
        public void FitArena(bool fixedSize)
        {
            Rect sa = Screen.safeArea;
            float fracW = sa.width / Mathf.Max(1f, Screen.width), fracH = sa.height / Mathf.Max(1f, Screen.height);
            float m2 = 2f * Config.ArenaMargin;

            if (fixedSize)
            {
                Vector2 a = Config.OnlineArenaSize;
                camSize = Mathf.Max((a.y + m2) / (2f * fracH), (a.x + m2) / (2f * cam.aspect * fracW));
                Config.ArenaSize = a;
            }
            else
            {
                camSize = Config.CameraSize;
                float uw = 2f * camSize * cam.aspect * fracW, uh = 2f * camSize * fracH;
                Config.ArenaSize = new Vector2(Mathf.Max(6f, uw - m2), Mathf.Max(4f, uh - m2));
            }
            cam.orthographicSize = camSize;

            float visH = 2f * camSize, visW = visH * cam.aspect;

            float offX = (sa.center.x / Screen.width - 0.5f) * visW;
            float offY = (sa.center.y / Screen.height - 0.5f) * visH;
            camPos = new Vector3(-offX, -offY, -10f);
        }

        void Update()
        {
            if (!Input.GetKeyDown(KeyCode.Escape)) return;   // also the Android back button
            if (GameWorld.Current != null) { if (GameWorld.Current.Paused) Resume(); else Pause(); }
            else if (UI.Current is MainMenuScreen) Application.Quit();
            else if (!(UI.Current is DialogScreen)) UI.Show<MainMenuScreen>();
        }

        /// <summary>Options > Graphics > Frame rate. 120 only helps on phones with a fast display.</summary>
        public void ApplyFrameRate()
        {
            int[] rates = { 30, 60, 120 };
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = rates[Mathf.Clamp(SaveManager.Data.frameRate, 0, 2)];
        }

        /// <summary>Short vibration (player hit). Options > Sound > Vibration.</summary>
        public void Vibrate()
        {
#if UNITY_ANDROID || UNITY_IOS
            if (SaveManager.Data.vibration) Handheld.Vibrate();
#endif
        }

        public void StartMode(ModeDef mode) { Session.Start(mode); }

        public void Pause()
        {
            var w = GameWorld.Current;
            if (w == null || w.Paused) return;
            if (w.State != GameWorld.Phase.Countdown && w.State != GameWorld.Phase.Playing) return;
            w.SetPaused(true);
            UI.ShowOverlay<PauseScreen>();
        }

        /// <summary>Continue after a pause - with a short countdown so you can get back into your tilt pose.</summary>
        public void Resume()
        {
            if (GameWorld.Current != null) GameWorld.Current.Resume();
            UI.Hide<PauseScreen>();
        }

        public void QuitToMenu()
        {
            if (GameWorld.Current != null && GameWorld.Current.Mode.Id == GameModeId.Tutorial)
            { SaveManager.Data.tutorialDone = true; SaveManager.Save(); }   // leaving the tutorial = skipping it
            Session.Abort();
            UI.Show<MainMenuScreen>();
        }
    }
}
