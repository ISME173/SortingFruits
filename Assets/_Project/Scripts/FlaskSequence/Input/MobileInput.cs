using System;
using UnityEngine;

namespace _Project.Scripts.FruitsSequence.Input
{
    public class MobileInput : MonoBehaviour, IInput
    {
        public event Action<Vector3> OnTriggerDown;

        private void Update()
        {
            if (UnityEngine.Input.touchCount > 0)
            {
                for (int i = 0; i < UnityEngine.Input.touchCount; i++)
                {
                    Touch touch = UnityEngine.Input.GetTouch(i);
                    if (touch.phase == TouchPhase.Began)
                    {
                        OnTriggerDown?.Invoke(touch.position);
                    }
                }
            }
        }
    }
}
