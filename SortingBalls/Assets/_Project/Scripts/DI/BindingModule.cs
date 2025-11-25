using Reflex.Core;
using UnityEngine;

namespace _Project.Scripts.DI
{
    public abstract class BindingModule : MonoBehaviour
    {
        public abstract void Bind(ContainerBuilder containerBuilder);
    }
}
