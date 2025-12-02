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
        //[SerializeField, Min(0)] private int _interstitialAdvShowDelaySeconds = 60;

        public void InstallBindings(ContainerBuilder containerBuilder)
        {
            containerBuilder.AddSingleton(new YgSaves(), typeof(ISaves));
            containerBuilder.AddSingleton(new YgAdvertising(), typeof(IAdvertising));
            containerBuilder.AddSingleton(new YgGameEvents(), typeof(IGameEvents));
            containerBuilder.AddSingleton(new YgLanguageInfo(), typeof(ILanguageInfo));
        }
    }
}
