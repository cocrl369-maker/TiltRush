using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TiltRush
{
    public abstract class UIScreen : MonoBehaviour
    {
        protected UIManager UI;
        protected virtual bool HasVignette { get { return false; } }   // menus: dark edges for depth
        public void Init(UIManager ui) { UI = ui; if (HasVignette) UIKit.AddVignette(transform); Build(); }
        protected abstract void Build();
        public virtual void OnShow() { }
    }

    /// <summary>Owns the canvas and all screens. To add a screen: write a UIScreen class and AddScreen it below.</summary>
    public class UIManager : MonoBehaviour
    {
        Canvas canvas;
        readonly Dictionary<Type, UIScreen> screens = new Dictionary<Type, UIScreen>();
        public UIScreen Current { get; private set; }
        UIScreen belowOverlay;   // the screen under an overlay (e.g. the HUD under the pause menu)
        Text fpsLabel;
        float fpsSmooth = 60f;

        public void Build()
        {
            var cgo = new GameObject("Canvas");
            cgo.transform.SetParent(transform, false);
            canvas = cgo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var sc = cgo.AddComponent<CanvasScaler>();
            sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            sc.referenceResolution = new Vector2(1920, 1080);
            sc.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            sc.matchWidthOrHeight = 0.5f;
            cgo.AddComponent<GraphicRaycaster>();

            if (EventSystem.current == null)
            {
                var es = new GameObject("EventSystem");
                es.transform.SetParent(transform, false);
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
            }

            AddScreen<MainMenuScreen>();
            AddScreen<ModeSelectScreen>();
            AddScreen<OptionsScreen>();
            AddScreen<HowToScreen>();
            AddScreen<WardrobeScreen>();
            AddScreen<LeaderboardScreen>();
            AddScreen<CampaignScreen>();
            AddScreen<TiltSetupScreen>();
            AddScreen<HudScreen>();
            AddScreen<PauseScreen>();     // keep overlays/dialog last so they render on top
            AddScreen<DialogScreen>();

            // FPS counter (Options > Interface > Show FPS), always on top
            fpsLabel = UIKit.Label(canvas.transform, "", 28, Neon.Lime);
            UIKit.Anchor(fpsLabel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -8), new Vector2(200, 40));
        }

        void Update()
        {
            if (fpsLabel == null) return;
            bool on = SaveManager.Data.showFps;
            if (fpsLabel.enabled != on) fpsLabel.enabled = on;
            if (!on) return;
            float dt = Mathf.Max(0.0001f, Time.unscaledDeltaTime);
            fpsSmooth = Mathf.Lerp(fpsSmooth, 1f / dt, 0.05f);
            fpsLabel.text = Mathf.RoundToInt(fpsSmooth) + " FPS";
        }

        T AddScreen<T>() where T : UIScreen
        {
            var rt = UIKit.Stretch(UIKit.Rect(typeof(T).Name, canvas.transform));
            var s = rt.gameObject.AddComponent<T>();
            rt.gameObject.AddComponent<ScreenFade>();
            s.Init(this);
            screens[typeof(T)] = s;
            s.gameObject.SetActive(false);
            return s;
        }

        public T Get<T>() where T : UIScreen { return (T)screens[typeof(T)]; }

        /// <summary>Show one screen and hide all others.</summary>
        public T Show<T>() where T : UIScreen
        {
            var target = screens[typeof(T)];
            foreach (var s in screens.Values)
                if (s != target) s.GetComponent<ScreenFade>().FadeOut();   // old screens fade out, then switch off
            belowOverlay = null;
            return Activate<T>();
        }

        /// <summary>Show a screen on top of the current ones (e.g. pause over HUD).</summary>
        public T ShowOverlay<T>() where T : UIScreen
        {
            if (!(Current is T)) belowOverlay = Current;
            return Activate<T>();
        }

        public void Hide<T>() where T : UIScreen
        {
            var s = screens[typeof(T)];
            s.GetComponent<ScreenFade>().FadeOut();
            if (Current == s) { Current = belowOverlay; belowOverlay = null; }
        }

        T Activate<T>() where T : UIScreen
        {
            var s = (T)screens[typeof(T)];
            var fade = s.GetComponent<ScreenFade>();
            bool visible = s.gameObject.activeSelf && !fade.FadingOut;   // e.g. a dialog that only gets new text
            s.gameObject.SetActive(true);
            if (!visible) fade.FadeIn();
            s.OnShow();
            Current = s;
            return s;
        }

        public void ShowDialog(string title, string body, params UIStack.Item[] buttons)
        {
            Get<DialogScreen>().Set(title, body, buttons);
            Show<DialogScreen>();
        }
    }

    /// <summary>
    /// Soft screen transitions: a new screen fades in and grows slightly into place, the old one fades out quickly
    /// (and stops taking clicks immediately). Uses unscaled time, so it also works while the game is paused.
    /// </summary>
    public class ScreenFade : MonoBehaviour
    {
        const float InTime = 0.22f, OutTime = 0.14f;
        CanvasGroup group;
        float t = 1f;
        bool fadingOut;

        public bool FadingOut { get { return fadingOut; } }

        void Awake()
        {
            group = GetComponent<CanvasGroup>();
            if (group == null) group = gameObject.AddComponent<CanvasGroup>();
        }

        public void FadeIn()
        {
            fadingOut = false; t = SaveManager.Data.menuAnimations ? 0f : 1f;   // Options > Interface > Animations
            group.blocksRaycasts = true; group.interactable = true;
            Apply();
        }

        public void FadeOut()
        {
            if (!gameObject.activeSelf || fadingOut) return;
            if (!SaveManager.Data.menuAnimations) { gameObject.SetActive(false); return; }
            fadingOut = true; t = 0f;
            group.blocksRaycasts = false; group.interactable = false;
        }

        void Update()
        {
            if (t >= 1f && !fadingOut) return;
            t += Time.unscaledDeltaTime / (fadingOut ? OutTime : InTime);
            Apply();
            if (fadingOut && t >= 1f)
            {
                fadingOut = false;
                gameObject.SetActive(false);
                group.alpha = 1f; transform.localScale = Vector3.one; group.blocksRaycasts = true; group.interactable = true;
            }
        }

        void Apply()
        {
            float k = Mathf.Clamp01(t);
            if (fadingOut)
            {
                group.alpha = 1f - k;
                transform.localScale = Vector3.one * Mathf.Lerp(1f, 1.015f, k);
            }
            else
            {
                float e = 1f - (1f - k) * (1f - k) * (1f - k);   // ease out
                group.alpha = e;
                transform.localScale = Vector3.one * Mathf.Lerp(0.97f, 1f, e);
            }
        }
    }
}
