using _Project.Scripts.FruitsSequence.Input;
using Reflex.Core;
using UnityEngine;

namespace _Project.Scripts.FruitsSequence
{
    public class FrinksSequenceInstaller : MonoBehaviour, IInstaller
    {
        [Header("References")]
        [SerializeField] private Camera _mainCamera;

        [Header("Settings")]
        [SerializeField] private FrinkItemsMover.MovingSettings _movingSettings;

        [Header("Input assets")]
        [SerializeField] private DesktopInput _desktopInputPrefab;
        [SerializeField] private MobileInput _mobileInputPrefab;

        private FrinkItemsMover _frinkItemsMover;

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

            _frinkItemsMover = new FrinkItemsMover(_mainCamera, _movingSettings, input);

            containerBuilder.AddSingleton(_frinkItemsMover);
        }
    }
}
