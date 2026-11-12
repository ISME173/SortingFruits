using _Project.Scripts.FruitsSequence.Input;
using Reflex.Core;
using UnityEngine;

namespace _Project.Scripts.FlaskSequence
{
    public class FlaskSequenceInstaller : MonoBehaviour, IInstaller
    {
        [Header("References")]
        [SerializeField] private Camera _mainCamera;
        [SerializeField] private LevelCreator _levelCreator;
        [Header("Settings")]
        [SerializeField] private FlaskItemsMover.MovingSettings _movingSettings;

        [Header("Input assets")]
        [SerializeField] private DesktopInput _desktopInputPrefab;
        [SerializeField] private MobileInput _mobileInputPrefab;

        private FlaskItemsMover _frinkItemsMover;

        private void OnDestroy()
        {
            _frinkItemsMover?.Dispose();
        }

        public void InstallBindings(ContainerBuilder containerBuilder)
        {
            IInput input = null;

            if (SystemInfo.deviceType == DeviceType.Desktop)
            {
                DesktopInput desktopInput = Instantiate(_desktopInputPrefab);
                DontDestroyOnLoad(desktopInput.gameObject);

                input = desktopInput;

                containerBuilder.AddSingleton(desktopInput, typeof(IInput));
            }
            else if (SystemInfo.deviceType == DeviceType.Handheld)
            {
                MobileInput mobileInput = Instantiate(_mobileInputPrefab);
                DontDestroyOnLoad(mobileInput.gameObject);

                input = mobileInput;

                containerBuilder.AddSingleton(mobileInput, typeof(IInput));
            }
            else
            {
                Debug.LogError("Invalid device type! Supported only mobile and desktop");
            }

            _frinkItemsMover = new FlaskItemsMover(_mainCamera, _movingSettings, input, _levelCreator);

            containerBuilder.AddSingleton(_levelCreator);
            containerBuilder.AddSingleton(_frinkItemsMover);
        }
    }
}
