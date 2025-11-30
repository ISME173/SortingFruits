using LitMotion;
using LitMotion.Extensions;
using Reflex.Attributes;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.FlaskSequence.Education
{
    public class EducationView : MonoBehaviour, IDisposable
    {
        private readonly List<GameObject> CanMoveViews = new List<GameObject>();
        private readonly List<GameObject> CantMoveViews = new List<GameObject>();

        [Header("Settings")]
        [SerializeField, Min(0)] private float _pointerMoveToFlaskTime;
        [SerializeField] private Ease _pointerMoveEase;
        [SerializeField, Min(-1)] private int _loopsCount;
        [SerializeField] private LoopType _loopType;

        [Header("Assets")]
        [SerializeField] private GameObject _pointerPrefab;
        [SerializeField] private GameObject _canMovePrefab;
        [SerializeField] private GameObject _cantMovePrefab;

        private GameObject _currentPointer;
        private MotionHandle _movePointerHandle;

        public void Dispose()
        {
            for (int i = 0; i < CanMoveViews.Count; i++)
                Destroy(CanMoveViews[i].gameObject);
            CanMoveViews.Clear();

            for (int i = 0; i < CantMoveViews.Count; i++)
                Destroy(CantMoveViews[i].gameObject);
            CantMoveViews.Clear();

            _movePointerHandle.TryCancel();

            if (_currentPointer != null)
                Destroy(_currentPointer.gameObject);
        }

        public void MovePointer(Transform start, Transform end)
        {
            _movePointerHandle.TryCancel();

            _currentPointer ??= Instantiate(_pointerPrefab);
            _currentPointer.transform.position = start.position;

            _movePointerHandle = LMotion.Create(start.position, end.position, _pointerMoveToFlaskTime)
                .WithEase(_pointerMoveEase)
                .WithLoops(_loopsCount, _loopType)
                .BindToPosition(_currentPointer.transform);
        }

        public void SetCanMovePoints(List<Transform> canMovePoints)
        {
            for (int i = 0; i < CanMoveViews.Count; i++)
                Destroy(CanMoveViews[i].gameObject);
            CanMoveViews.Clear();

            List<GameObject> canMoveViews = new List<GameObject>();
            for (int i = 0; i < canMovePoints.Count; i++)
                canMoveViews.Add(Instantiate(_canMovePrefab, canMovePoints[i].position, Quaternion.identity));

            CanMoveViews.AddRange(canMoveViews);
        }

        public void SetCantMovePoints(List<Transform> cantMovePoints)
        {
            for (int i = 0; i < CantMoveViews.Count; i++)
                Destroy(CantMoveViews[i].gameObject);
            CantMoveViews.Clear();

            List<GameObject> cantMoveViews = new List<GameObject>();
            for (int i = 0; i < cantMovePoints.Count; i++)
                cantMoveViews.Add(Instantiate(_cantMovePrefab, cantMovePoints[i].position, Quaternion.identity));

            CantMoveViews.AddRange(cantMoveViews);
        }

        [Inject]
        private void Initialize(EducationController educationController, LevelCreator levelCreator, FlaskItemsMover flaskItemsMover)
        {
            educationController.Initialize(levelCreator, flaskItemsMover);
        }
    }
}
