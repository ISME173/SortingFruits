using System;
using UnityEngine;

namespace _Project.Scripts.FruitsSequence.Input
{
    public class DesktopInput : MonoBehaviour, IInput
    {
        [SerializeField] private KeyCode _checkKeyCode;

        public event Action<Vector3> OnTriggerDown;

        private void Update()
        {
            if (UnityEngine.Input.GetKeyDown(_checkKeyCode))
                OnTriggerDown?.Invoke(UnityEngine.Input.mousePosition);
        }
    }
}
