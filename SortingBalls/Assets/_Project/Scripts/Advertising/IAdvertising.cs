using System;

namespace _Project.Scripts.Advertising
{
    public interface IAdvertising : IDisposable
    {
        public void Initialize();
        public void ShowRewarded(Action onSuccess, Action onError);
        public void ShowInterstitial(Action onSuccess, Action onError);
        public bool CanShowInterstitial();
    }
}
