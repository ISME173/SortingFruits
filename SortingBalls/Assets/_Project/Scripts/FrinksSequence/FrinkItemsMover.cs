using _Project.Scripts.FruitsSequence.Input;
using LitMotion;
using System;
using UnityEngine;

namespace _Project.Scripts.FruitsSequence
{
    public class FrinkItemsMover : IDisposable
    { 
        private readonly Camera CurrentCamera;
        private readonly MovingSettings MoveSettings;
        private readonly IInput CurrentInput;

        private Frink _lastFrinkHit;

        public FrinkItemsMover(Camera mainCamera, MovingSettings movingSettings, IInput input)
        {
            CurrentCamera = mainCamera;
            MoveSettings = movingSettings;
            CurrentInput = input;

            CurrentInput.OnTriggerDown += SearchFrink;
        }

        private void SearchFrink(Vector3 screenPosition)
        {
            Ray ray = CurrentCamera.ScreenPointToRay(screenPosition);
            RaycastHit2D hit2D = Physics2D.GetRayIntersection(ray, Mathf.Infinity);

            if (hit2D.collider == null)
                return;

            if (hit2D.collider.TryGetComponent(out Frink frink))
            {
                _lastFrinkHit = frink;

                Debug.Log($"{typeof(Frink)} founded: {_lastFrinkHit.name}");
            }
        }

        public void Dispose()
        {
            CurrentInput.OnTriggerDown -= SearchFrink;
        }

        [Serializable] 
        public struct MovingSettings
        {
            [SerializeField, Min(0)] private float _moveTime;
            [SerializeField] private Ease _moveEase;

            public float MoveTime => _moveTime;
            public Ease MoveEase => _moveEase;
        }
    }
}
