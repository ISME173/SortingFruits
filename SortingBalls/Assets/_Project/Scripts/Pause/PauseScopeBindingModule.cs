using _Project.Scripts.DI;
using Reflex.Core;
using UnityEngine;

namespace _Project.Scripts.Pause
{
    public class PauseScopeBindingModule : BindingModule
    {
        [SerializeField] private PauseView _pauseView;

        private PauseController _pauseController;

        private void OnDestroy()
        {
            _pauseController?.Dispose();
        }

        public override void Bind(ContainerBuilder containerBuilder)
        {
            _pauseController = new PauseController(_pauseView);

            containerBuilder.AddSingleton(_pauseController);
            containerBuilder.AddSingleton(_pauseView);
        }
    }
}
