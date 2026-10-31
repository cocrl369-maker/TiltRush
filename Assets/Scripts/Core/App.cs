using UnityEngine;

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
            Application.targetFrameRate = 60;
            Screen.autorotateToPortrait = false;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = true;
            Screen.orientation = ScreenOrientation.AutoRotation;   // landscape only
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Input.multiTouchEnabled = true;

            SaveManager.Load();
            SetupCamera();

            Audio = gameObject.AddComponent<AudioManager>();
            gameObject.AddComponent<NeonBackground>();
            Online = new MockOnlineService();          // <- swap for a real backend later
            Session = new SessionController(this);

            UI = gameObject.AddComponent<UIManager>();
            UI.Build();
            UI.Show<MainMenuScreen>();
            Audio.Next();
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
        float shakeMag, shakeTime, shakeDur = 1f;

        /// <summary>Short camera shake (nuke, shockwave, death). Can be switched off in the options.</summary>
        public void Shake(float magnitude, float duration)
        {
            if (!SaveManager.Data.screenShake) return;
            float current = shakeMag * Mathf.Clamp01(shakeTime / shakeDur);
            if (magnitude < current) return;                      // don't let a small shake cancel a big one
            shakeMag = magnitude; shakeDur = Mathf.Max(0.01f, duration); shakeTime = duration;
        }

        void LateUpdate()
        {
            cam.orthographicSize = Config.CameraSize;
            if (GameWorld.Current == null) FitArena();    // the arena size only changes between rounds
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
        void FitArena()
        {
            float visH = 2f * Config.CameraSize, visW = visH * cam.aspect;
            Rect sa = Screen.safeArea;
            float usableW = visW * sa.width / Screen.width;
            float usableH = visH * sa.height / Screen.height;
            Config.ArenaSize = new Vector2(
                Mathf.Max(6f, usableW - 2f * Config.ArenaMargin),
                Mathf.Max(4f, usableH - 2f * Config.ArenaMargin));

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

        public void StartMode(ModeDef mode) { Session.Start(mode); }

        public void Pause()
        {
            if (GameWorld.Current == null || GameWorld.Current.State == GameWorld.Phase.Done) return;
            GameWorld.Current.SetPaused(true);
            UI.ShowOverlay<PauseScreen>();
        }

        public void Resume()
        {
            if (GameWorld.Current != null) GameWorld.Current.SetPaused(false);
            UI.Hide<PauseScreen>();
        }

        public void QuitToMenu()
        {
            Session.Abort();
            UI.Show<MainMenuScreen>();
        }
    }
}
