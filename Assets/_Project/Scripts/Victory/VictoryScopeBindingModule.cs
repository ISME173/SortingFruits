using _Project.Scripts.DI;
using Reflex.Core;
using UnityEngine;

namespace _Project.Scripts.Victory
{
    public class VictoryScopeBindingModule : BindingModule
    {
        [SerializeField] private VictoryView _victoryView;

        private VictoryController _victoryController;

        private void OnDestroy()
        {
            _victoryController?.Dispose();
        }

        public override void Bind(ContainerBuilder containerBuilder)
        {
            _victoryController = new VictoryController(_victoryView);

            containerBuilder.AddSingleton(_victoryController);
            containerBuilder.AddSingleton(_victoryView);
        }
    }
}
