using _Project.Scripts.DI;
using Reflex.Core;
using UnityEngine;

namespace _Project.Scripts.FlaskSequence.Education
{
    public class EducationBindingModule : BindingModule
    {
        [SerializeField] private EducationView _educationView;

        private EducationController _educationController;

        private void OnDestroy()
        {
            _educationController?.Dispose();
            _educationView?.Dispose();
        }

        public override void Bind(ContainerBuilder containerBuilder)
        {
            _educationController = new EducationController(_educationView);

            containerBuilder.AddSingleton(_educationController);
            containerBuilder.AddSingleton(_educationView);
        }
    }
}
