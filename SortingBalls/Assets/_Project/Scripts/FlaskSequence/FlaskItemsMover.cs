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
        private readonly List<Flask> UsingFilling = new List<Flask>();
        private readonly Camera CurrentCamera;
        private readonly MovingSettings MoveSettings;
        private readonly IInput CurrentInput;
        private readonly float FrinkUpYPosition;

        private Flask _currentFrink;
        private MotionHandle _moveUpFrinkHandle;
        private MotionHandle _moveDownFrinkHandle;

        public FlaskItemsMover(Camera mainCamera, MovingSettings movingSettings, IInput input)
        {
            CurrentCamera = mainCamera;
            MoveSettings = movingSettings;
            CurrentInput = input;

            FrinkUpYPosition = MoveSettings.StartYFrinksPositions + MoveSettings.MoveYOffsetInSelected;

            CurrentInput.OnTriggerDown += SearchFrink;
        }

        private void SearchFrink(Vector3 screenPosition)
        {
            Ray ray = CurrentCamera.ScreenPointToRay(screenPosition);
            RaycastHit2D hit2D = Physics2D.GetRayIntersection(ray, Mathf.Infinity);

            if (hit2D.collider == null)
            {
                MoveDownFrink(_currentFrink);
                _currentFrink = null;
                return;
            }

            if (hit2D.collider.TryGetComponent(out Flask frink))
            {
                if (_currentFrink == null)
                {
                    _currentFrink = frink;

                    _moveUpFrinkHandle.TryCancel();
                    MoveUpFrink(_currentFrink);
                }
                else if (_currentFrink != frink)
                {
                    _moveDownFrinkHandle.TryCancel();
                    MoveDownFrink(_currentFrink);

                    if (_currentFrink.PeekFirstItem() == null)
                    {
                        MoveUpFrink(frink);
                    }
                    else
                    {
                        TryMoveItems(_currentFrink, frink);
                    }
                }

                _currentFrink = frink;
            }
        }

        private void TryMoveItems(Flask startFrink, Flask endFrink)
        {
            if (UsingFilling.Contains(startFrink) || UsingFilling.Contains(endFrink))
            {
                return;
            }

            List<Item> itemsForMove = new List<Item>();

            while (startFrink.PeekFirstItem() != null)
            {
                itemsForMove.Add(startFrink.GetFirstItem());
            }

            if (itemsForMove.Count > 0)
            {
                UsingFilling.Add(endFrink);
                UsingFilling.Add(startFrink);
            }

            MotionSequenceBuilder moveItemsSequence = LSequence.Create();

            for (int i = 0; i < itemsForMove.Count; i++)
            {
                startFrink.GetFirstItem();

                Item itemForMove = itemsForMove[i];

                float moveTimeInOneMotion = MoveSettings.ItemsMoveTime / 3;

                itemForMove.transform.SetParent(startFrink.SlotForSelectItems);
                moveItemsSequence.Append(LMotion.Create(itemForMove.transform.localPosition, Vector3.zero, moveTimeInOneMotion)
                    .WithEase(MoveSettings.ItemsMoveEase)
                    .WithOnComplete(() => itemForMove.transform.SetParent(endFrink.SlotForSelectItems))
                    .BindToLocalPosition(itemForMove.transform));

                moveItemsSequence.Append(LMotion.Create(itemForMove.transform.localPosition, Vector3.zero, moveTimeInOneMotion)
                    .WithEase(MoveSettings.ItemsMoveEase)
                    .WithOnComplete(() =>
                    {
                        itemForMove.transform.SetParent(endFrink.GetFirstEmptySlotTransform());
                        endFrink.TryAddItem(itemForMove);
                    })
                    .BindToLocalPosition(itemForMove.transform));

                moveItemsSequence.Append(LMotion.Create(itemForMove.transform.localPosition, Vector3.zero, moveTimeInOneMotion)
                    .WithEase(MoveSettings.ItemsMoveEase)
                    .WithOnComplete(() =>
                    {
                        if (i == itemsForMove.Count - 1)
                        {
                            UsingFilling.Remove(endFrink);
                            UsingFilling.Remove(startFrink);
                        }
                    })
                    .BindToLocalPosition(itemForMove.transform));
            }

            moveItemsSequence.Run();
        }

        private void MoveUpFrink(Flask frink)
        {
            if (frink != null)
            {
                _moveUpFrinkHandle = LMotion.Create(frink.transform.localPosition, new Vector3(frink.transform.localPosition.x, FrinkUpYPosition, 0), MoveSettings.FrinkMoveTime)
                  .WithEase(MoveSettings.FrinkMoveEase)
                  .WithCancelOnError()
                  .BindToLocalPosition(frink.transform);
            }
        }

        private void MoveDownFrink(Flask frink)
        {
            if (frink != null)
            {
                _moveDownFrinkHandle = _moveDownFrinkHandle = LMotion.Create(frink.transform.localPosition, new Vector3(frink.transform.localPosition.x, MoveSettings.StartYFrinksPositions, 0), MoveSettings.FrinkMoveTime)
                  .WithEase(MoveSettings.FrinkMoveEase)
                  .WithCancelOnError()
                  .BindToLocalPosition(frink.transform);
            }
        }

        public void Dispose()
        {
            CurrentInput.OnTriggerDown -= SearchFrink;
        }

        [Serializable]
        public struct MovingSettings
        {
            [Header("Move items")]
            [SerializeField, Min(0)] private float _itemsMoveTime;
            [SerializeField] private Ease _itemsMoveEase;

            [Header("Move frink")]
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
