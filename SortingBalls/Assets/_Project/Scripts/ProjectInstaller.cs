using _Project.Scripts.Advertising;
using _Project.Scripts.GameEvents;
using _Project.Scripts.Localization;
using _Project.Scripts.Saves;
using Reflex.Core;
using UnityEngine;

namespace _Project.Scripts
{
    public class ProjectInstaller : MonoBehaviour, IInstaller
    {
        [SerializeField, Min(0)] private int _interstitialAdvShowDelaySeconds = 60;

        public void InstallBindings(ContainerBuilder containerBuilder)
        {
#if !UNITY_EDITOR
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
#endif
            containerBuilder.AddSingleton(new PlayerPrefsSaves(), typeof(ISaves));

            var advertising = new YgAdvertising(_interstitialAdvShowDelaySeconds);
            advertising.Initialize();
            containerBuilder.AddSingleton(advertising, typeof(IAdvertising));

            containerBuilder.AddSingleton(new DevGameEvents(), typeof(IGameEvents));
            containerBuilder.AddSingleton(new DevLanguageInfo(), typeof(ILanguageInfo));
        }
    }
}
