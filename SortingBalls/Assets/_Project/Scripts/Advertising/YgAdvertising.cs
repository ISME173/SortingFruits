using System;
using YG;

namespace _Project.Scripts.Advertising
{
    public class YgAdvertising : IAdvertising
    {
        public bool CanShowInterstitial()
        {
            return YG2.isTimerAdvCompleted;
        }

        public void ShowInterstitial(Action onSuccess, Action onError)
        {
            YG2.onCloseInterAdvWasShow += OnInterstitialShowed;
            YG2.onErrorInterAdv += OnIntersitialError;

            YG2.InterstitialAdvShow();

            void OnInterstitialShowed(bool isShowedResult)
            {
                if (isShowedResult)
                {
                    if (onSuccess != null)
                        onSuccess();
                }
                else
                {
                    if (onError != null)
                        onError();
                }

                YG2.onCloseInterAdvWasShow -= OnInterstitialShowed;
                YG2.onErrorInterAdv -= OnIntersitialError;
            }

            void OnIntersitialError()
            {
                if (onError != null)
                    onError();

                YG2.onCloseInterAdvWasShow -= OnInterstitialShowed;
                YG2.onErrorInterAdv -= OnIntersitialError;
            }
        }

        public void ShowRewarded(Action onSuccess, Action onError)
        {
            YG2.onErrorRewardedAdv += OnErrorReward;

            YG2.RewardedAdvShow("Any reward", () =>
            {
                if (onSuccess != null)
                    onSuccess();

                YG2.onErrorRewardedAdv -= OnErrorReward;
            });

            void OnErrorReward()
            {
                if (onError != null)
                    onError();

                YG2.onErrorRewardedAdv -= OnErrorReward;
            }
        }
    }
}
