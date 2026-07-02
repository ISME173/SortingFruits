using System;
using YandexMobileAds;
using YandexMobileAds.Base;
using UnityEngine;
using System.Collections.Generic;
using _Project.Scripts.Utils;

namespace _Project.Scripts.Advertising
{
    public class YgAdvertising : IAdvertising
    {
        private const string RewardedAdUnitId = "ID R-M-19507938-2";
        private const string InterstitialAdUnitId = "ID R-M-19507938-1";

        private readonly float _interstitialReloadDelaySeconds;

        private readonly HashSet<Action> _onRewardedAdSuccessCallbacks = new HashSet<Action>();
        private readonly HashSet<Action> _onRewardedAdErrorCallbacks = new HashSet<Action>();
        private readonly HashSet<Action> _onInterstitialAdSuccessCallbacks = new HashSet<Action>();
        private readonly HashSet<Action> _onInterstitialAdErrorCallbacks = new HashSet<Action>();

        private RewardedAdLoader _rewardedAdLoader;
        private RewardedAd _rewardedAd;

        private InterstitialAdLoader _interstitialAdLoader;
        private Interstitial _interstitial;
        private bool _canShowInterstitial = true;

        public YgAdvertising(float interstitialReloadDelaySeconds = 60f)
        {
            _interstitialReloadDelaySeconds = interstitialReloadDelaySeconds;
        }

        public bool CanShowInterstitial() => _canShowInterstitial && _interstitial != null;

        public void Dispose()
        {
            UnsubscribeRewardedAdEvents();
            UnsubscribeInterstitialAdEvents();

            _rewardedAd?.Destroy();
            _rewardedAd = null;
            _rewardedAdLoader = null;

            _interstitial?.Destroy();
            _interstitial = null;
            _interstitialAdLoader = null;
        }

        public void Initialize()
        {
            YandexAds.SetAgeRestricted(true);

            _rewardedAdLoader = new RewardedAdLoader();
            RequestRewardedAd();

            _interstitialAdLoader = new InterstitialAdLoader();
            RequestInterstitialAd();
        }

        public void ShowInterstitial(Action onSuccess, Action onError)
        {
            if (_canShowInterstitial == false)
            {
                //Debug.LogWarning("Interstitial ad is on cooldown");
                onError?.Invoke();
                return;
            }

            if (_interstitial == null)
            {
                //Debug.LogError("Interstitial ad is not ready yet");
                onError?.Invoke();
                return;
            }

            if (onSuccess != null)
                _onInterstitialAdSuccessCallbacks.Add(onSuccess);

            if (onError != null)
                _onInterstitialAdErrorCallbacks.Add(onError);

            _interstitial.Show();
        }

        public void ShowRewarded(Action onSuccess, Action onError)
        {
            if (_rewardedAd == null)
            {
                //Debug.LogError("Rewarded ad is not ready yet");
                onError?.Invoke();
                return;
            }

            if (onSuccess != null)
                _onRewardedAdSuccessCallbacks.Add(onSuccess);

            if (onError != null)
                _onRewardedAdErrorCallbacks.Add(onError);

            _rewardedAd.Show();
        }

        private void RequestRewardedAd()
        {
            if (_rewardedAd != null)
            {
                UnsubscribeRewardedAdEvents();
                _rewardedAd.Destroy();
                _rewardedAd = null;
            }

            _rewardedAdLoader.LoadAd(
                new AdRequest(RewardedAdUnitId),
                onLoaded: HandleRewardedAdLoaded,
                onFailed: HandleRewardedAdFailedToLoad);
        }

        private void RequestInterstitialAd()
        {
            if (_interstitial != null)
            {
                UnsubscribeInterstitialAdEvents();
                _interstitial.Destroy();
                _interstitial = null;
            }

            _interstitialAdLoader.LoadAd(
                new AdRequest(InterstitialAdUnitId),
                onLoaded: HandleInterstitialAdLoaded,
                onFailed: HandleInterstitialAdFailedToLoad);
        }

        private void UnsubscribeRewardedAdEvents()
        {
            if (_rewardedAd == null)
                return;

            _rewardedAd.OnAdClicked -= HandleRewardedAdClicked;
            _rewardedAd.OnAdShown -= HandleRewardedAdShown;
            _rewardedAd.OnAdFailedToShow -= HandleRewardedAdFailedToShow;
            _rewardedAd.OnAdImpression -= HandleRewardedAdImpression;
            _rewardedAd.OnAdDismissed -= HandleRewardedAdDismissed;
            _rewardedAd.OnRewarded -= HandleRewardedAdRewarded;
        }

        private void UnsubscribeInterstitialAdEvents()
        {
            if (_interstitial == null)
                return;

            _interstitial.OnAdClicked -= HandleInterstitialAdClicked;
            _interstitial.OnAdShown -= HandleInterstitialAdShown;
            _interstitial.OnAdFailedToShow -= HandleInterstitialAdFailedToShow;
            _interstitial.OnAdImpression -= HandleInterstitialAdImpression;
            _interstitial.OnAdDismissed -= HandleInterstitialAdDismissed;
        }

        #region Rewarded ad callback handlers

        private void HandleRewardedAdLoaded(RewardedAd rewardedAd)
        {
            _rewardedAd = rewardedAd;

            _rewardedAd.OnAdClicked += HandleRewardedAdClicked;
            _rewardedAd.OnAdShown += HandleRewardedAdShown;
            _rewardedAd.OnAdFailedToShow += HandleRewardedAdFailedToShow;
            _rewardedAd.OnAdImpression += HandleRewardedAdImpression;
            _rewardedAd.OnAdDismissed += HandleRewardedAdDismissed;
            _rewardedAd.OnRewarded += HandleRewardedAdRewarded;
        }

        private void HandleRewardedAdFailedToLoad(AdFailedToLoadEventArgs args)
        {
            //Debug.LogError($"Rewarded ad failed to load: {args.Message}");
            RequestRewardedAd();
        }

        private void HandleRewardedAdClicked(object sender, EventArgs args)
        {
            //Debug.Log("Rewarded ad clicked");
        }

        private void HandleRewardedAdShown(object sender, EventArgs args)
        {
            //Debug.Log("Rewarded ad shown");
        }

        private void HandleRewardedAdDismissed(object sender, EventArgs args)
        {
            //Debug.Log("Rewarded ad dismissed");

            UnsubscribeRewardedAdEvents();
            _rewardedAd.Destroy();
            _rewardedAd = null;

            RequestRewardedAd();
        }

        private void HandleRewardedAdImpression(object sender, ImpressionData impressionData)
        {
            //Debug.Log("Rewarded ad impression");
        }

        private void HandleRewardedAdRewarded(object sender, Reward args)
        {
            //Debug.Log("Rewarded ad rewarded");

            InvokeAndClear(_onRewardedAdSuccessCallbacks);
            _onRewardedAdErrorCallbacks.Clear();
        }

        private void HandleRewardedAdFailedToShow(object sender, AdFailureEventArgs args)
        {
            //Debug.LogError($"Rewarded ad failed to show: {args.Message}");

            InvokeAndClear(_onRewardedAdErrorCallbacks);
            _onRewardedAdSuccessCallbacks.Clear();

            RequestRewardedAd();
        }

        #endregion

        #region Interstitial ad callback handlers

        private void HandleInterstitialAdLoaded(Interstitial interstitial)
        {
            _interstitial = interstitial;

            _interstitial.OnAdClicked += HandleInterstitialAdClicked;
            _interstitial.OnAdShown += HandleInterstitialAdShown;
            _interstitial.OnAdFailedToShow += HandleInterstitialAdFailedToShow;
            _interstitial.OnAdImpression += HandleInterstitialAdImpression;
            _interstitial.OnAdDismissed += HandleInterstitialAdDismissed;
        }

        private void HandleInterstitialAdFailedToLoad(AdFailedToLoadEventArgs args)
        {
            //Debug.LogError($"Interstitial ad failed to load: {args.Message}");
            RequestInterstitialAd();
        }

        private void HandleInterstitialAdClicked(object sender, EventArgs args)
        {
            //Debug.Log("Interstitial ad clicked");
        }

        private void HandleInterstitialAdShown(object sender, EventArgs args)
        {
            //Debug.Log("Interstitial ad shown");

            _canShowInterstitial = false;
            Timer.After(_interstitialReloadDelaySeconds, () => _canShowInterstitial = true);
        }

        private void HandleInterstitialAdDismissed(object sender, EventArgs args)
        {
            //Debug.Log("Interstitial ad dismissed");

            InvokeAndClear(_onInterstitialAdSuccessCallbacks);
            _onInterstitialAdErrorCallbacks.Clear();

            UnsubscribeInterstitialAdEvents();
            _interstitial.Destroy();
            _interstitial = null;

            RequestInterstitialAd();
        }

        private void HandleInterstitialAdImpression(object sender, ImpressionData impressionData)
        {
            //Debug.Log("Interstitial ad impression");
        }

        private void HandleInterstitialAdFailedToShow(object sender, AdFailureEventArgs args)
        {
            //Debug.LogError($"Interstitial ad failed to show: {args.Message}");

            InvokeAndClear(_onInterstitialAdErrorCallbacks);
            _onInterstitialAdSuccessCallbacks.Clear();

            RequestInterstitialAd();
        }

        #endregion

        private static void InvokeAndClear(HashSet<Action> callbacks)
        {
            foreach (var callback in callbacks)
                callback?.Invoke();

            callbacks.Clear();
        }
    }
}
