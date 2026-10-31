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
        }

        T AddScreen<T>() where T : UIScreen
        {
            var rt = UIKit.Stretch(UIKit.Rect(typeof(T).Name, canvas.transform));
            var s = rt.gameObject.AddComponent<T>();
            s.Init(this);
            screens[typeof(T)] = s;
            s.gameObject.SetActive(false);
            return s;
        }

        public T Get<T>() where T : UIScreen { return (T)screens[typeof(T)]; }

        /// <summary>Show one screen and hide all others.</summary>
        public T Show<T>() where T : UIScreen
        {
            foreach (var s in screens.Values) s.gameObject.SetActive(false);
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
            s.gameObject.SetActive(false);
            if (Current == s) { Current = belowOverlay; belowOverlay = null; }
        }

        T Activate<T>() where T : UIScreen
        {
            var s = (T)screens[typeof(T)];
            s.gameObject.SetActive(true);
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
}
