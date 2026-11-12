using System;
using _Project.Scripts.Utils;
using GamePush;
using GamePush.Native;
using LitMotion;

namespace _Project.Scripts.Advertising
{
    public class GamepushAdvertising : IAdvertising
    {
        private readonly float _interstitialReloadSeconds;

        private bool _canShowInterstitial = true;
        private MotionHandle _interstitialCooldown;

        public GamepushAdvertising(float interstitialReloadSeconds)
        {
            _interstitialReloadSeconds = interstitialReloadSeconds;
        }

        public void Initialize()
        {
        }

        public void Dispose()
        {
            _interstitialCooldown.TryCancel();
        }

        public bool CanShowInterstitial()
        {
            if (!_canShowInterstitial || GP_Ads.IsFullscreenPlaying())
                return false;

#if UNITY_EDITOR
            return true;
#else
            return GP_Ads.IsFullscreenAvailable();
#endif
        }

        public void ShowInterstitial(Action onSuccess, Action onError)
        {
            if (!CanShowInterstitial())
            {
                onError?.Invoke();
                return;
            }

            var completed = false;

            void Complete(bool success)
            {
                if (completed)
                    return;

                completed = true;

                if (!success)
                {
                    onError?.Invoke();
                    return;
                }

                onSuccess?.Invoke();
                _canShowInterstitial = false;
                _interstitialCooldown.TryCancel();
                _interstitialCooldown = Timer.After(_interstitialReloadSeconds, () => _canShowInterstitial = true);
            }

            GP_Ads.ShowFullscreen(onFullscreenClose: success => Complete(success));

#if UNITY_EDITOR
            if (!completed && !GP_AdsStub.Enabled && !GP_Play2Web.Enabled && !GamePushHost.UseNativeCore)
                Complete(true);
#endif
        }

        public void ShowRewarded(Action onSuccess, Action onError)
        {
            if (GP_Ads.IsRewardPlaying())
            {
                onError?.Invoke();
                return;
            }

#if !UNITY_EDITOR
            if (!GP_Ads.IsRewardedAvailable())
            {
                onError?.Invoke();
                return;
            }
#endif

            var completed = false;

            void Complete(bool success)
            {
                if (completed)
                    return;

                completed = true;

                if (success)
                    onSuccess?.Invoke();
                else
                    onError?.Invoke();
            }

            GP_Ads.ShowRewarded(
                onRewardedReward: _ => Complete(true),
                onRewardedClose: success =>
                {
                    if (!completed)
                        Complete(success);
                });
        }
    }
}
