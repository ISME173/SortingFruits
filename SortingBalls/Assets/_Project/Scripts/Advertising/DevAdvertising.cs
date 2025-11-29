using _Project.Scripts.Utils;
using System;
using UnityEngine;

namespace _Project.Scripts.Advertising
{
    public class DevAdvertising : IAdvertising
    {
        private readonly float InterstitialAdvReloadingTimer;

        private bool _canShowInterstitial = true;

        public DevAdvertising(float interstitialAdvReloadingTimer)
        {
            InterstitialAdvReloadingTimer = interstitialAdvReloadingTimer;
        }

        public bool CanShowInterstitial() => _canShowInterstitial;

        public void ShowInterstitial(Action onSuccess, Action onError)
        {
            if (_canShowInterstitial == false)
            {
                Debug.Log("<color=yellow>Interstitial adv on reloading!</color>");

                if (onError != null)
                    onError();

                return;
            }

            Debug.Log("<color=green>Show interstitial</color>");

            if (onSuccess != null)
                onSuccess();

            _canShowInterstitial = false;
            Timer.After(InterstitialAdvReloadingTimer, () => _canShowInterstitial = true);
        }

        public void ShowRewarded(Action onSuccess, Action onError)
        {
            Debug.Log($"<color=white>Rewarded adv showed!</color>");

            if (onSuccess != null)
                onSuccess();
        }
    }
}
