using System;
using System.Collections.Generic;
using UnityEngine;
#if TILTRUSH_IAP || TILTRUSH_IAP5
using UnityEngine.Purchasing;
#endif
#if TILTRUSH_IAP
using UnityEngine.Purchasing.Extension;
#endif

namespace TiltRush
{
    /// <summary>
    /// In-app purchase "Remove ads" (Unity IAP, package "In-App Purchasing" = com.unity.purchasing).
    /// Switches on automatically when the package is installed (TiltRush.asmdef sets TILTRUSH_IAP5 for
    /// version 5.x = current Unity IAP, or TILTRUSH_IAP for the old version 4.x).
    /// The product must exist in the Google Play Console (Monetize > Products > In-app products) with the
    /// ID Config.RemoveAdsProductId ("remove_ads"). Google restores the purchase automatically on reinstall.
    /// </summary>
    public class Purchases : MonoBehaviour
#if TILTRUSH_IAP
        , IDetailedStoreListener
#endif
    {
        public bool Available { get; private set; }     // store connected, product found
        public string Price { get; private set; } = "";
        public static bool AdsRemoved { get { return SaveManager.Data.adsRemoved; } }

#if TILTRUSH_IAP
        IStoreController store;

        void Start()
        {
            var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
            builder.AddProduct(Config.RemoveAdsProductId, ProductType.NonConsumable);
            UnityPurchasing.Initialize(this, builder);
        }

        public void BuyRemoveAds()
        {
            if (store == null) { App.I.UI.ShowDialog("SHOP", "The shop is not available right now.\nPlease try again later.", new UIStack.Item("OK", Neon.Gray, () => App.I.UI.Show<MainMenuScreen>())); return; }
            store.InitiatePurchase(Config.RemoveAdsProductId);
        }

        public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            store = controller;
            var p = controller.products.WithID(Config.RemoveAdsProductId);
            Available = p != null && p.availableToPurchase;
            if (p != null && p.metadata != null) Price = p.metadata.localizedPriceString;
            if (p != null && p.hasReceipt) Unlock(false);          // bought earlier (e.g. after reinstalling)
        }

        public void OnInitializeFailed(InitializationFailureReason error) { Debug.LogWarning("TILT RUSH shop: " + error); }
        public void OnInitializeFailed(InitializationFailureReason error, string message) { Debug.LogWarning("TILT RUSH shop: " + error + " " + message); }

        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
        {
            if (args.purchasedProduct.definition.id == Config.RemoveAdsProductId) Unlock(true);
            return PurchaseProcessingResult.Complete;
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureReason reason) { }
        public void OnPurchaseFailed(Product product, PurchaseFailureDescription description) { }
#elif TILTRUSH_IAP5
        // ---------------------------------------------------------------- Unity IAP 5 (current version)
        StoreController store;

        async void Start()
        {
            try
            {
                store = UnityIAPServices.StoreController();
                store.OnProductsFetched += products =>
                {
                    foreach (var p in products)
                    {
                        if (p == null || p.definition == null || p.definition.id != Config.RemoveAdsProductId) continue;
                        Available = p.availableToPurchase;
                        if (p.metadata != null) Price = p.metadata.localizedPriceString;
                    }
                    store.FetchPurchases();                           // already bought earlier? (e.g. after reinstalling)
                };
                store.OnPurchasesFetched += orders =>
                {
                    foreach (var order in orders.ConfirmedOrders)
                        foreach (var item in order.CartOrdered.Items())
                            if (item.Product.definition.id == Config.RemoveAdsProductId) Unlock(false);
                };
                store.OnPurchasePending += order =>
                {
                    bool removeAds = false;
                    foreach (var item in order.CartOrdered.Items())
                        if (item.Product.definition.id == Config.RemoveAdsProductId) removeAds = true;
                    store.ConfirmPurchase(order);
                    if (removeAds) Unlock(true);
                };

                await store.Connect();
                store.FetchProducts(new List<ProductDefinition> { new ProductDefinition(Config.RemoveAdsProductId, ProductType.NonConsumable) });
            }
            catch (Exception e)
            {
                Debug.LogWarning("TILT RUSH shop: " + e.Message);
            }
        }

        public void BuyRemoveAds()
        {
            if (store == null || !Available) { NotAvailable(); return; }
            store.PurchaseProduct(Config.RemoveAdsProductId);
        }

        static void NotAvailable()
        {
            App.I.UI.ShowDialog("SHOP", "The shop is not available right now.\nPlease try again later.",
                new UIStack.Item("OK", Neon.Gray, () => App.I.UI.Show<MainMenuScreen>()));
        }
#else
        public void BuyRemoveAds()
        {
            App.I.UI.ShowDialog("SHOP", "In-app purchases are not set up yet.", new UIStack.Item("OK", Neon.Gray, () => App.I.UI.Show<MainMenuScreen>()));
        }
#endif

        void Unlock(bool thank)
        {
            if (SaveManager.Data.adsRemoved) return;
            SaveManager.Data.adsRemoved = true;
            SaveManager.Save();
            if (thank)
                App.I.UI.ShowDialog("THANK YOU!", "Ads are gone for good.\nThanks for supporting TILT RUSH!",
                    new UIStack.Item("OK", Neon.Lime, () => App.I.UI.Show<MainMenuScreen>()));
        }
    }
}
