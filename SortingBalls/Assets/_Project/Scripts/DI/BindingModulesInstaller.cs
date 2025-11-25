using NaughtyAttributes;
using Reflex.Core;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace _Project.Scripts.DI
{
    public class BindingModulesInstaller : MonoBehaviour, IInstaller
    {
        [SerializeField] private bool _searchBindingModulesInOnValidate = true;
        [HideIf(nameof(_searchBindingModulesInOnValidate))]
        [SerializeField] private List<BindingModule> _bindingModules = new();

        private void OnValidate()
        {
            if (_searchBindingModulesInOnValidate)
            {
                _bindingModules = FindObjectsOfType<BindingModule>().ToList();   
            }
        }

        public void InstallBindings(ContainerBuilder containerBuilder)
        {
            for (int i = 0; i < _bindingModules.Count; i++)
            {
                _bindingModules[i].Bind(containerBuilder);
            }
        }
    }
}
