using _Project.Scripts.Saves;
using Reflex.Core;
using UnityEngine;

namespace _Project.Scripts
{
    public class ProjectInstaller : MonoBehaviour, IInstaller
    {
        public void InstallBindings(ContainerBuilder containerBuilder)
        {
            containerBuilder.AddSingleton(new PlayerPrefsSaves(), typeof(ISaves));
        }
    }
}
