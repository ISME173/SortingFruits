using _Project.Scripts.FruitsSequence.Input;
using LitMotion;
using LitMotion.Extensions;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.FlaskSequence
{
    public class FlaskItemsMover : IDisposable
    {
        private readonly Dictionary<Flask, MotionHandle> MovingFlasks = new Dictionary<Flask, MotionHandle>();
        private readonly HashSet<Flask> UsingFilling = new HashSet<Flask>();
        private readonly Camera CurrentCamera;
        private readonly MovingSettings MoveSettings;
        private readonly IInput CurrentInput;
        private readonly float FrinkUpYPosition;

        private Flask _currentFlask;
        private MotionHandle _moveUpFrinkHandle;
        private MotionHandle _moveDownFrinkHandle;

        public FlaskItemsMover(Camera mainCamera, MovingSettings movingSettings, IInput input)
        {
            CurrentCamera = mainCamera;
            MoveSettings = movingSettings;
            CurrentInput = input;

            FrinkUpYPosition = MoveSettings.StartYFrinksPositions + MoveSettings.MoveYOffsetInSelected;

            CurrentInput.OnTriggerDown += SearchFlask;
        }

        private void SearchFlask(Vector3 screenPosition)
        {
            Ray ray = CurrentCamera.ScreenPointToRay(screenPosition);
            RaycastHit2D hit2D = Physics2D.GetRayIntersection(ray, Mathf.Infinity);

            if (hit2D.collider != null && hit2D.collider.TryGetComponent(out Flask flask))
            {
                if (UsingFilling.Contains(flask))
                    return;

                if (_currentFlask == null)
                {
                    _currentFlask = flask;
                    MoveUpFlask(_currentFlask);
                    return;
                }
                else if (_currentFlask != flask)
                {
                    if (TryMoveItems(_currentFlask, flask))
                    {
                        MoveDownFlask(_currentFlask);
                        _currentFlask = null;
                        return;
                    }
                }
            }

            if (_currentFlask != null)
                MoveDownFlask(_currentFlask);

            _currentFlask = null;
        }

        private bool TryMoveItems(Flask startFlask, Flask endFlask)
        {
            if (startFlask.PeekFirstItem() == null)
                return false;

            if (endFlask.PeekFirstItem() != null)
                return false;

            string moveItemName = startFlask.PeekFirstItem().ItemName;

            Item firstItemInEndFlask = endFlask.PeekFirstItem();
            string firstItemNameInEndFlask = firstItemInEndFlask != null ? firstItemInEndFlask.ItemName : string.Empty;

            if (firstItemNameInEndFlask != string.Empty && moveItemName != firstItemNameInEndFlask)
                return false;

            List<Item> itemsToMove = new List<Item>();
            int maxItemsToMove = endFlask.FreeSlotsCount;

            UsingFilling.Add(endFlask);

            while (maxItemsToMove > 0)
            {
                Item peekFirstItem = startFlask.PeekFirstItem();
                if (peekFirstItem == null)
                    break;

                if (peekFirstItem.ItemName != firstItemNameInEndFlask && firstItemNameInEndFlask != string.Empty)
                    break;

                itemsToMove.Add(startFlask.GetFirstItem());
                maxItemsToMove--;
            }

            for (int i = 0; i < itemsToMove.Count; i++)
            {
                if (i == itemsToMove.Count - 1)
                {
                    MoveOneItem(itemsToMove[i], () =>
                    {
                        UsingFilling.Remove(endFlask);
                    });
                }
                else
                {
                    MoveOneItem(itemsToMove[i], null);
                }

                endFlask.TryAddItem(itemsToMove[i]);
            }

            return true;

            void MoveOneItem(Item item, Action callback)
            {
                // Точки пути в мировых координатах
                Transform firstEmptySlot = endFlask.GetFirstEmptySlotTransform();
                Vector3 p0 = item.transform.position;
                Vector3 p1 = startFlask.SlotForSelectItems.position;
                Vector3 p2 = endFlask.SlotForSelectItems.position;
                Vector3 p3 = firstEmptySlot.position;

                // Для корректного порядка отрисовки переносим в "слот выбора", сохраняя мировую позицию
                item.transform.SetParent(startFlask.SlotForSelectItems.transform, false);
                item.transform.localScale = Vector3.one;

                MotionSequenceBuilder moveItemSequence = LSequence.Create();

                moveItemSequence
                    .Append(LMotion.Create(p0, p1, MoveSettings.ItemsMoveTime)
                        .WithEase(MoveSettings.ItemsMoveEase)
                        .WithCancelOnError()
                        .BindToPosition(item.transform))
                    .Append(LMotion.Create(p1, p2, MoveSettings.ItemsMoveTime)
                        .WithEase(MoveSettings.ItemsMoveEase)
                        .WithCancelOnError()
                        .BindToPosition(item.transform))
                    .Append(LMotion.Create(p2, p3, MoveSettings.ItemsMoveTime)
                        .WithEase(MoveSettings.ItemsMoveEase)
                        .WithCancelOnError()
                        .WithOnComplete(() =>
                        {
                            // В конце привязываем к целевой ячейке без скачков
                            item.transform.SetParent(firstEmptySlot, true);
                            item.transform.localScale = Vector3.one;

                            //item.transform.localPosition = Vector3.zero;

                            if (callback != null)
                                callback();
                        })
                        .BindToPosition(item.transform));

                moveItemSequence.Run();
            }
        }

        private void MoveUpFlask(Flask flask)
        {
            if (flask != null)
            {
                if (MovingFlasks.TryGetValue(flask, out MotionHandle motionHandle))
                {
                    motionHandle.TryCancel();
                    MovingFlasks.Remove(flask);
                }

                _moveUpFrinkHandle = LMotion.Create(flask.transform.localPosition, new Vector3(flask.transform.localPosition.x, FrinkUpYPosition, 0), MoveSettings.FrinkMoveTime)
                  .WithEase(MoveSettings.FrinkMoveEase)
                  .WithCancelOnError()
                  .WithOnComplete(() =>
                  {
                      if (MovingFlasks.ContainsKey(flask))
                          MovingFlasks.Remove(flask);
                  })
                  .BindToLocalPosition(flask.transform);

                MovingFlasks.Add(flask, _moveUpFrinkHandle);
            }
        }

        private void MoveDownFlask(Flask flask)
        {
            if (flask != null)
            {
                if (MovingFlasks.TryGetValue(flask, out MotionHandle motionHandle))
                {
                    motionHandle.TryCancel();
                    MovingFlasks.Remove(flask);
                }

                _moveDownFrinkHandle = LMotion.Create(flask.transform.localPosition, new Vector3(flask.transform.localPosition.x, MoveSettings.StartYFrinksPositions, 0), MoveSettings.FrinkMoveTime)
                  .WithEase(MoveSettings.FrinkMoveEase)
                  .WithCancelOnError()
                  .WithOnComplete(() =>
                  {
                      if (MovingFlasks.ContainsKey(flask))
                          MovingFlasks.Remove(flask);
                  })
                  .BindToLocalPosition(flask.transform);

                MovingFlasks.Add(flask, _moveDownFrinkHandle);
            }
        }

        public void Dispose()
        {
            CurrentInput.OnTriggerDown -= SearchFlask;
        }

        [Serializable]
        public struct MovingSettings
        {
            [Header("Move items")]
            [SerializeField, Min(0)] private float _itemsMoveTime;
            [SerializeField] private Ease _itemsMoveEase;

            [Header("Move flask")]
            [SerializeField, Min(0)] private float _frinkMoveTime;
            [SerializeField] private Ease _frinkMoveEase;
            [SerializeField] private float _moveYOffsetInSelected;
            [SerializeField] private float _startYFrinksPositions;

            public float ItemsMoveTime => _itemsMoveTime;
            public Ease ItemsMoveEase => _itemsMoveEase;

            public float FrinkMoveTime => _frinkMoveTime;
            public Ease FrinkMoveEase => _frinkMoveEase;
            public float MoveYOffsetInSelected => _moveYOffsetInSelected;
            public float StartYFrinksPositions => _startYFrinksPositions;
        }
    }
}
