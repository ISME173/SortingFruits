using _Project.Scripts.FlaskSequence.Education;
using _Project.Scripts.FruitsSequence.Input;
using LitMotion;
using LitMotion.Extensions;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;

namespace _Project.Scripts.FlaskSequence
{
    public class FlaskItemsMover : IDisposable
    {
        private readonly Stack<Move> MovesInLevel = new Stack<Move>();
        private readonly Dictionary<Flask, float> FlaskYPositionInUp = new Dictionary<Flask, float>();
        private readonly Dictionary<Flask, float> FlaskYPositionInDown = new Dictionary<Flask, float>();
        private readonly Dictionary<Flask, MotionHandle> MovingFlasks = new Dictionary<Flask, MotionHandle>();
        private readonly HashSet<Flask> UsingFilling = new HashSet<Flask>();
        private readonly HashSet<MotionHandle> MovingItemHandlers = new HashSet<MotionHandle>();
        private readonly Camera CurrentCamera;
        private readonly MovingSettings MoveSettings;
        private readonly IInput CurrentInput;
        private readonly LevelCreator LevelCreator;

        private CancellationTokenSource _moveItemsCts = new CancellationTokenSource();
        private Flask _currentFlask;
        private MotionHandle _moveUpFrinkHandle;
        private MotionHandle _moveDownFrinkHandle;

        public event Action OnAnyItemMovingEnd, OnAnyItemMovingStart;
        public event Action<Move> OnMove;

        public bool IsMovingAnyItem => MovingItemHandlers.Count > 0;

        public FlaskItemsMover(Camera mainCamera, MovingSettings movingSettings, IInput input, LevelCreator levelCreator)
        {
            CurrentCamera = mainCamera;
            MoveSettings = movingSettings;
            CurrentInput = input;
            LevelCreator = levelCreator;

            CurrentInput.OnTriggerDown += SearchFlask;
            LevelCreator.LevelCompleted += OnLevelComplete;
            LevelCreator.LevelCreated += OnLevelCreated;
        }

        public bool TryCancelLastMove()
        {
            Move lastMove = MovesInLevel.Peek();

            if (CanCancelLastMove() == false ||
                (UsingFilling.Contains(lastMove.EndFlask) || UsingFilling.Contains(lastMove.StartFlask)))
                return false;

            if (lastMove.EndFlask.IsFilled || lastMove.StartFlask.IsFilled)
                return false;

            MovesInLevel.Pop();

            return TryMoveItems(lastMove.EndFlask, lastMove.StartFlask, true);
        }

        public bool CanCancelLastMove()
        {
            return MovesInLevel.Count > 0;
        }

        private void OnLevelComplete(LevelData levelData)
        {
            if (!_moveItemsCts.IsCancellationRequested)
                _moveItemsCts.Cancel();

            _moveItemsCts.Dispose();
            _moveItemsCts = new CancellationTokenSource();
        }

        private void OnLevelCreated(LevelData levelData)
        {
            foreach (var handle in MovingItemHandlers)
                handle.TryCancel();
            MovingItemHandlers.Clear();

            foreach (var key in MovingFlasks.Keys)
                MovingFlasks[key].TryCancel();
            MovingFlasks.Clear();

            MovesInLevel.Clear();
        }

        private void SearchFlask(Vector3 screenPosition)
        {
            if (EventSystem.current.IsPointerOverGameObject())
                return;

            Ray ray = CurrentCamera.ScreenPointToRay(screenPosition);
            RaycastHit2D hit2D = Physics2D.GetRayIntersection(ray, Mathf.Infinity);

            if (hit2D.collider != null && hit2D.collider.TryGetComponent(out Flask flask))
            {
                if ((UsingFilling.Contains(flask) || MovingFlasks.ContainsKey(flask)) || flask.IsFilled)
                    return;

                if (_currentFlask == null)
                {
                    _currentFlask = flask;
                    MoveUpFlask(_currentFlask);
                    return;
                }
                else if (_currentFlask != flask)
                {
                    MoveDownFlask(_currentFlask);
                    if (TryMoveItems(_currentFlask, flask))
                    {
                        _currentFlask = null;
                    }
                    else
                    {
                        MoveDownFlask(_currentFlask);
                        _currentFlask = flask;
                        MoveUpFlask(_currentFlask);
                    }

                    return;
                }
            }

            if (_currentFlask != null)
                MoveDownFlask(_currentFlask);

            _currentFlask = null;
        }

        private bool TryMoveItems(Flask startFlask, Flask endFlask, bool moveAnyway = false)
        {
            if (startFlask.PeekFirstItem() == null)
                return false;

            if (endFlask.FreeSlotsCount == 0)
                return false;

            string moveItemName = startFlask.PeekFirstItem().ItemName;

            Item firstItemInEndFlask = endFlask.PeekFirstItem();
            string firstItemNameInEndFlask = firstItemInEndFlask != null ? firstItemInEndFlask.ItemName : string.Empty;

            if (moveAnyway == false)
            {
                if (firstItemNameInEndFlask != string.Empty && moveItemName != firstItemNameInEndFlask)
                    return false;
            }

            List<Item> itemsToMove = new List<Item>();
            int maxItemsToMove = endFlask.FreeSlotsCount;

            UsingFilling.Add(endFlask);

            while (maxItemsToMove > 0)
            {
                Item peekFirstItem = startFlask.PeekFirstItem();
                if (peekFirstItem == null)
                    break;

                if (moveAnyway == false)
                {
                    if (peekFirstItem.ItemName != firstItemNameInEndFlask && firstItemNameInEndFlask != string.Empty)
                        break;
                }

                Item firstItemInStartFlask = startFlask.GetFirstItem();
                itemsToMove.Add(firstItemInStartFlask);
                firstItemNameInEndFlask = firstItemInStartFlask.ItemName;

                maxItemsToMove--;
            }

            MoveAllItems(_moveItemsCts.Token);
            return true;

            async void MoveAllItems(CancellationToken ct)
            {
                for (int i = 0; i < itemsToMove.Count; i++)
                {
                    if (ct.IsCancellationRequested)
                    {
                        UsingFilling.Remove(endFlask);
                        break;
                    }

                    if (i == itemsToMove.Count - 1)
                    {
                        MoveOneItem(itemsToMove[i], () =>
                        {
                            UsingFilling.Remove(endFlask);

                            Move move = new Move(startFlask, endFlask);
                            MovesInLevel.Push(move);

                            if (endFlask.IsFilled)
                            {
                                endFlask.PlaySfxOnFilledEffect();
                                endFlask.PlayVfxOnFilledEffect();
                            }

                            OnMove?.Invoke(move);
                        });
                    }
                    else
                    {
                        MoveOneItem(itemsToMove[i], null);
                    }

                    endFlask.TryAddItem(itemsToMove[i]);

                    try
                    {
                        await Task.Delay(MoveSettings.MillisecondsDelayBetweenMoveItems, ct);
                    }
                    catch (OperationCanceledException)
                    {
                        UsingFilling.Remove(endFlask);
                        break;
                    }
                }
            }

            void MoveOneItem(Item item, Action callback)
            {
                Transform firstEmptySlot = endFlask.GetFirstEmptySlotTransform();

                Vector3 p0 = item.transform.position;
                Vector3 p1 = startFlask.SlotForSelectItems.position;
                Vector3 p2 = endFlask.SlotForSelectItems.position;
                Vector3 p3 = firstEmptySlot.position;

                MotionSequenceBuilder moveItemSequence = LSequence.Create();
                MotionHandle? motionHandle = null;

                item.transform.SetParent(null);

                moveItemSequence
                    .Append(LMotion.Create(p0, p1, MoveSettings.ItemsMoveTime)
                        .WithEase(MoveSettings.ItemsMoveEase)
                        .WithCancelOnError()
                        .BindToPosition(item.transform))
                    .Append(LMotion.Create(p1, p2, MoveSettings.ItemsMoveTime)
                        .WithCancelOnError()
                        .WithEase(MoveSettings.ItemsMoveEase)
                        .WithOnComplete(() =>
                        {
                            LMotion.Create(0, 1, MoveSettings.ItemsMoveTime + MoveSettings.OffsetForPlaySfxAfterItemMovedInSlot)
                            .WithOnComplete(() => endFlask.PlaySfxOnMovedItemInFlask())
                            .RunWithoutBinding();
                        })
                        .BindToPosition(item.transform))
                    .Append(LMotion.Create(p2, p3, MoveSettings.ItemsMoveTime)
                        .WithCancelOnError()
                        .WithEase(Ease.OutBounce)
                        .WithOnComplete(() =>
                        {
                            if (motionHandle != null)
                            {
                                MovingItemHandlers.Remove(motionHandle.Value);

                                if (IsMovingAnyItem == false)
                                    OnAnyItemMovingEnd?.Invoke();
                            }

                            item.transform.SetParent(firstEmptySlot, true);
                            item.transform.localPosition = Vector3.zero;
                            callback?.Invoke();
                        })
                        .Bind((progress) =>
                        {
                            if (item == null)
                                return;

                            item.transform.position = progress;
                        }));

                motionHandle = moveItemSequence.Run();

                if (!IsMovingAnyItem)
                    OnAnyItemMovingStart?.Invoke();

                MovingItemHandlers.Add(motionHandle.Value);
            }
        }

        private void MoveUpFlask(Flask flask)
        {
            if (flask != null)
            {
                flask.PlaySfxOnClickedToFlask();

                if (MovingFlasks.TryGetValue(flask, out MotionHandle motionHandle))
                {
                    motionHandle.TryCancel();
                    MovingFlasks.Remove(flask);
                }

                if (FlaskYPositionInUp.ContainsKey(flask) == false)
                {
                    FlaskYPositionInUp.Add(flask, flask.transform.localPosition.y + MoveSettings.MoveYOffsetInSelected);
                }

                _moveUpFrinkHandle = LMotion.Create(flask.transform.localPosition, new Vector3(flask.transform.localPosition.x, FlaskYPositionInUp[flask], 0), MoveSettings.FrinkMoveTime)
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
                flask.PlaySfxOnClickedToFlask();

                if (MovingFlasks.TryGetValue(flask, out MotionHandle motionHandle))
                {
                    motionHandle.TryCancel();
                    MovingFlasks.Remove(flask);
                }

                if (FlaskYPositionInDown.ContainsKey(flask) == false)
                {
                    FlaskYPositionInDown.Add(flask, flask.transform.localPosition.y - MoveSettings.MoveYOffsetInSelected);
                }

                _moveDownFrinkHandle = LMotion.Create(flask.transform.localPosition, new Vector3(flask.transform.localPosition.x, FlaskYPositionInDown[flask], 0), MoveSettings.FrinkMoveTime)
                  .WithEase(MoveSettings.FrinkMoveEase)
                  .WithCancelOnError()
                  .WithOnComplete(() =>
                  {
                      if (MovingFlasks.ContainsKey(flask))
                          MovingFlasks.Remove(flask);
                  })
                  .Bind((progress) =>
                  {
                      if (flask == null)
                          return;

                      flask.transform.localPosition = progress;
                  });

                MovingFlasks.Add(flask, _moveDownFrinkHandle);
            }
        }

        public void Dispose()
        {
            LevelCreator.LevelCompleted -= OnLevelComplete;
            LevelCreator.LevelCreated -= OnLevelCreated;
            CurrentInput.OnTriggerDown -= SearchFlask;

            if (!_moveItemsCts.IsCancellationRequested)
                _moveItemsCts.Cancel();

            _moveItemsCts.Dispose();
        }

        [Serializable]
        public struct MovingSettings
        {
            [Header("Move items")]
            [SerializeField, Min(0)] private float _itemsMoveTime;
            [SerializeField] private Ease _itemsMoveEase;
            [SerializeField, Min(0)] private int _millisecondsDelayBetweenMoveItems;
            [Space]
            [SerializeField] private float _offsetForPlaySfxAfterItemMovedInSlot;

            [Header("Move flask")]
            [SerializeField, Min(0)] private float _frinkMoveTime;
            [SerializeField] private Ease _frinkMoveEase;
            [SerializeField] private float _moveYOffsetInSelected;

            public float ItemsMoveTime => _itemsMoveTime;
            public Ease ItemsMoveEase => _itemsMoveEase;

            public float OffsetForPlaySfxAfterItemMovedInSlot => _offsetForPlaySfxAfterItemMovedInSlot;  

            public float FrinkMoveTime => _frinkMoveTime;
            public Ease FrinkMoveEase => _frinkMoveEase;
            public float MoveYOffsetInSelected => _moveYOffsetInSelected;
            public int MillisecondsDelayBetweenMoveItems => _millisecondsDelayBetweenMoveItems;
        }

        public struct Move
        {
            public readonly Flask StartFlask;
            public readonly Flask EndFlask;

            public Move(Flask startFlask, Flask endFlask)
            {
                StartFlask = startFlask;
                EndFlask = endFlask;
            }
        }
    }
}
