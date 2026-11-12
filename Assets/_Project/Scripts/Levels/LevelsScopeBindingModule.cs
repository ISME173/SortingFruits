using _Project.Scripts.DI;
using Reflex.Core;
using UnityEngine;

namespace _Project.Scripts.Levels
{
    public class LevelsScopeBindingModule : BindingModule
    {
        [SerializeField] private LevelsView _levelsView;

        private LevelsController _levelsController;

        private void OnDestroy()
        {
            _levelsController?.Dispose();
        }

        public override void Bind(ContainerBuilder containerBuilder)
        {
            _levelsController = new LevelsController(_levelsView);

            containerBuilder.AddSingleton(_levelsController);
            containerBuilder.AddSingleton(_levelsView);
        }
    }
}
