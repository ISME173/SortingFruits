using System;
using UnityEngine;

namespace _Project.Scripts.FruitsSequence.Input
{
    public interface IInput
    {
        public event Action<Vector3> OnTriggerDown;
    }
}
