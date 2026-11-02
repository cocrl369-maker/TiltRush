using UnityEngine;
#if TILTRUSH_ADS
using UnityEngine.Advertisements;
#endif

namespace TiltRush
{
    /// <summary>
    /// Ads (Unity Ads, package "Advertisement Legacy" = com.unity.ads). Switches on automatically when the package is
    /// installed (define TILTRUSH_ADS from TiltRush.asmdef) and Config.AdsGameIdAndroid is filled in.
    /// Rules:
    /// - an interstitial ad (skippable after ~5 s, set in the Unity dashboard) every Config.AdIntervalSeconds,
    /// - NEVER during a round, the tutorial, matchmaking or an online match - it waits for the next calm moment
    ///   (a menu / the result screen, at least AdCalmSeconds after the last screen change),
    /// - the first time the main menu appears, the player is asked whether ads may be personalised (EU / GDPR).
    /// </summary>
    public class AdManager : MonoBehaviour
#if TILTRUSH_ADS
        , IUnityAdsInitializationListener, IUnityAdsLoadListener, IUnityAdsShowListener
#endif
    {
        const float AdCalmSeconds = 1.2f;
        float sinceLast, calm, retry;
        bool ready, loaded, showing, asking;

        public bool Enabled { get { return ready; } }

        string GameId { get { return Application.platform == RuntimePlatform.IPhonePlayer ? Config.AdsGameIdIos : Config.AdsGameIdAndroid; } }
        string AdUnit { get { return Application.platform == RuntimePlatform.IPhonePlayer ? Config.AdsInterstitialIos : Config.AdsInterstitialAndroid; } }

        void Start()
        {
#if TILTRUSH_ADS
            if (string.IsNullOrEmpty(GameId)) { Debug.Log("TILT RUSH ads: no Game ID in Config.AdsGameIdAndroid - ads are off."); return; }
            if (!Advertisement.isSupported) return;
            ApplyConsent();
            Advertisement.Initialize(GameId, Config.AdsTestMode, this);
#else
            Debug.Log("TILT RUSH ads: package 'Advertisement Legacy' not installed - ads are off.");
#endif
        }

        /// <summary>Called on every screen change: an ad never pops up right while somebody taps a button.</summary>
        public void NotifyScreenChange() { calm = 0f; }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            sinceLast += dt;
            if (showing) return;

            bool busy = GameWorld.Current != null || (App.I.Session != null && App.I.Session.Busy) || !SaveManager.Data.tutorialDone;
            if (busy) { calm = 0f; return; }
            calm += dt;

            // first time in the main menu: ask about personalised ads
            if (!SaveManager.Data.adConsentAsked && !asking && calm > 0.6f && App.I.UI.Current is MainMenuScreen) { AskConsent(); return; }
            if (asking) return;

            if (!ready) return;
            if (retry > 0f) { retry -= dt; return; }
            if (sinceLast >= Config.AdIntervalSeconds && calm >= AdCalmSeconds) Show();
        }

        // ------------------------------------------------------------------ consent (GDPR)
        void AskConsent()
        {
            asking = true;
            App.I.UI.ShowDialog("ADS & PRIVACY",
                "TILT RUSH is free and shows a short ad every few minutes (never during a round).\n\n" +
                "May our ad partner Unity Ads use data from your device to show ads that fit you better?\n" +
                "You can change this any time in OPTIONS > PROFILE.",
                new UIStack.Item("YES", Neon.Lime, () => SetConsent(true)),
                new UIStack.Item("NO", Neon.Gray, () => SetConsent(false)));
        }

        void SetConsent(bool yes)
        {
            var d = SaveManager.Data;
            d.adConsentAsked = true; d.adConsent = yes;
            SaveManager.Save();
            ApplyConsent();
            asking = false;
            App.I.UI.Show<MainMenuScreen>();
        }

        /// <summary>Tells Unity Ads whether personalised ads are allowed (Options > Profile can change it).</summary>
        public void ApplyConsent()
        {
#if TILTRUSH_ADS
            string v = SaveManager.Data.adConsent ? "true" : "false";
            var gdpr = new MetaData("gdpr"); gdpr.Set("consent", v); Advertisement.SetMetaData(gdpr);
            var privacy = new MetaData("privacy"); privacy.Set("consent", v); Advertisement.SetMetaData(privacy);
#endif
        }

        // ------------------------------------------------------------------ showing
        void Show()
        {
#if TILTRUSH_ADS
            if (!loaded) { Advertisement.Load(AdUnit, this); retry = 10f; return; }
            showing = true; loaded = false;
            AudioListener.pause = true;                       // game sounds off while the ad plays
            Advertisement.Show(AdUnit, this);
#endif
        }

        void Done(bool success)
        {
            showing = false;
            AudioListener.pause = false;
            if (success) sinceLast = 0f; else retry = 30f;
#if TILTRUSH_ADS
            Advertisement.Load(AdUnit, this);                 // prepare the next one
#endif
        }

#if TILTRUSH_ADS
        public void OnInitializationComplete() { ready = true; Advertisement.Load(AdUnit, this); }
        public void OnInitializationFailed(UnityAdsInitializationError error, string message) { Debug.LogWarning("TILT RUSH ads: init failed: " + message); }
        public void OnUnityAdsAdLoaded(string placementId) { loaded = true; }
        public void OnUnityAdsFailedToLoad(string placementId, UnityAdsLoadError error, string message) { loaded = false; retry = 30f; }
        public void OnUnityAdsShowFailure(string placementId, UnityAdsShowError error, string message) { Done(false); }
        public void OnUnityAdsShowStart(string placementId) { }
        public void OnUnityAdsShowClick(string placementId) { }
        public void OnUnityAdsShowComplete(string placementId, UnityAdsShowCompletionState state) { Done(true); }
#endif
    }
}
