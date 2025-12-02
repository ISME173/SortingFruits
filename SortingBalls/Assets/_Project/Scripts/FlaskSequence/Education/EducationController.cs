using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace _Project.Scripts.FlaskSequence.Education
{
    public class EducationController : IDisposable
    {
        private readonly List<FlaskItemsMover.Move> MovesToCompleteLevel = new();
        private readonly EducationView EducationView;

        private int _currentMoveIndex = 0;
        private LevelCreator _levelCreator;
        private FlaskItemsMover _flaskItemsMover;

        public EducationController(EducationView educationView)
        {
            EducationView = educationView;
        }

        public bool IsCorrectMove(FlaskItemsMover.Move move)
        {
            FlaskItemsMover.Move currentMove = MovesToCompleteLevel[_currentMoveIndex];
            return (move.StartFlask == currentMove.StartFlask && move.EndFlask == currentMove.EndFlask);
        }

        public void Dispose()
        {
            if (_levelCreator != null)
            {
                _levelCreator.LevelCreated -= OnLevelCreated;

                foreach (var flask in _levelCreator.SpawnedFlasks)
                    flask.Collider2D.enabled = true;
            }

            if (_flaskItemsMover != null)
            {
                _flaskItemsMover.OnMove -= OnMove;
            }

            EducationView.Dispose();
        }

        public void Initialize(LevelCreator levelCreator, FlaskItemsMover flaskItemsMover)
        {
            _levelCreator = levelCreator;
            _flaskItemsMover = flaskItemsMover;

            if (_levelCreator != null)
            {
                _levelCreator.LevelCreated += OnLevelCreated;
            }

            if (_flaskItemsMover != null)
            {
                _flaskItemsMover.OnMove += OnMove;
            }
        }

        private void OnMove(FlaskItemsMover.Move move)
        {
            if (_levelCreator.CurrentLevelIndex != 0)
            {
                Dispose();
                return;
            }

            FlaskItemsMover.Move currentMove = MovesToCompleteLevel[_currentMoveIndex];
            if (move.StartFlask == currentMove.StartFlask && move.EndFlask == currentMove.EndFlask)
            {
                _currentMoveIndex++;

                if (_currentMoveIndex == MovesToCompleteLevel.Count)
                {
                    Dispose();
                    return;
                }

                FlaskItemsMover.Move nextMove = MovesToCompleteLevel[_currentMoveIndex];
                EducationView.MovePointer(nextMove.StartFlask.Center, nextMove.EndFlask.Center);

                foreach (var flask in _levelCreator.SpawnedFlasks)
                    flask.Collider2D.enabled = flask == nextMove.StartFlask || flask == nextMove.EndFlask;
            }
        }

        private void OnLevelCreated(LevelData levelData)
        {
            if (_levelCreator.CurrentLevelIndex != 0)
            {
                Dispose();
                return;
            }

            List<FlaskItemsMover.Move> movesForCompeleteLevel = new List<FlaskItemsMover.Move>();
            List<List<string>> flasks = levelData.Flasks;

            // 1) Решаем уровень по LevelData, получаем последовательность ходов (индексы колб)
            if (!TrySolve(levelData, out var moveIndices))
                return;

            // 2) Находим реальные объекты Flask в сцене и упорядочиваем так же, как создаёт LevelCreator
            var sceneFlasks = FindSceneFlasksForLevel(flasks.Count);
            if (sceneFlasks == null || sceneFlasks.Count != flasks.Count)
                return;

            // 3) Конвертируем ходы (индексы) в реальные Move
            foreach (var (from, to) in moveIndices)
            {
                var start = sceneFlasks[from];
                var end = sceneFlasks[to];
                if (start != null && end != null)
                {
                    movesForCompeleteLevel.Add(new FlaskItemsMover.Move(start, end));
                }
            }

            MovesToCompleteLevel.Clear();
            MovesToCompleteLevel.AddRange(movesForCompeleteLevel);

            FlaskItemsMover.Move currentMove = MovesToCompleteLevel[0];
            EducationView.MovePointer(currentMove.StartFlask.Center, currentMove.EndFlask.Center);

            foreach (var flask in _levelCreator.SpawnedFlasks)
                flask.Collider2D.enabled = flask == currentMove.StartFlask || flask == currentMove.EndFlask;
        }

        // Поиск колб текущего уровня в сцене и упорядочивание как в LevelCreator (по siblingIndex)
        private static List<Flask> FindSceneFlasksForLevel(int expectedCount)
        {
            var all = UnityEngine.Object.FindObjectsOfType<Flask>(includeInactive: false);
            if (all == null || all.Length == 0)
                return null;

            // Группируем по родителю, выбираем группу подходящего размера (в приоритете полный матч)
            var byParent = all.GroupBy(f => f.transform.parent).ToList();

            IGrouping<Transform, Flask> group =
                byParent.FirstOrDefault(g => g.Count() == expectedCount)
                ?? byParent.OrderByDescending(g => g.Count()).First();

            var ordered = group
                .OrderBy(f => f.transform.GetSiblingIndex())
                .ToList();

            // Если группа больше ожидаемой, берём первые expectedCount в порядке siblingIndex
            if (ordered.Count > expectedCount)
                ordered = ordered.Take(expectedCount).ToList();

            return ordered;
        }

        // BFS-решение с восстановлением пути, аналогично JsonLevelCreator.Solve, но возвращает последовательность ходов (индексы)
        private bool TrySolve(LevelData startLevel, out List<(int from, int to)> moves)
        {
            moves = null;

            int capacity = startLevel.FlaskCapacity;
            var start = Clone(startLevel.Flasks);
            if (IsSolved(start, capacity))
            {
                moves = new List<(int from, int to)>();
                return true;
            }

            string Encode(List<List<string>> state)
            {
                var parts = new List<string>(state.Count);
                for (int i = 0; i < state.Count; i++)
                {
                    parts.Add(string.Join(",", state[i]));
                }
                return string.Join("|", parts);
            }

            var startKey = Encode(start);

            var visited = new HashSet<string> { startKey };
            var queue = new Queue<List<List<string>>>();
            queue.Enqueue(start);

            // Для восстановления пути: ключ -> (родительский ключ, ход from,to)
            var parent = new Dictionary<string, (string prevKey, int from, int to)>();

            while (queue.Count > 0)
            {
                var state = queue.Dequeue();

                for (int fromIdx = 0; fromIdx < state.Count; fromIdx++)
                {
                    var from = state[fromIdx];
                    if (from.Count == 0) continue;

                    string topFruit = from[from.Count - 1];
                    int groupSize = 1;
                    for (int i = from.Count - 2; i >= 0; i--)
                    {
                        if (from[i] == topFruit) groupSize++;
                        else break;
                    }

                    for (int toIdx = 0; toIdx < state.Count; toIdx++)
                    {
                        if (toIdx == fromIdx) continue;

                        var to = state[toIdx];
                        if (to.Count >= capacity) continue;
                        if (to.Count > 0 && to[to.Count - 1] != topFruit) continue;

                        int free = capacity - to.Count;
                        int moveCount = Math.Min(groupSize, free);

                        var next = Clone(state);
                        var nFrom = next[fromIdx];
                        var nTo = next[toIdx];

                        for (int m = 0; m < moveCount; m++)
                        {
                            string val = nFrom[nFrom.Count - 1];
                            nFrom.RemoveAt(nFrom.Count - 1);
                            nTo.Add(val);
                        }

                        string key = Encode(next);
                        if (!visited.Add(key))
                            continue;

                        parent[key] = (Encode(state), fromIdx, toIdx);

                        if (IsSolved(next, capacity))
                        {
                            // Восстанавливаем путь
                            var path = new List<(int from, int to)>();
                            string curKey = key;
                            while (curKey != startKey)
                            {
                                var step = parent[curKey];
                                path.Add((step.from, step.to));
                                curKey = step.prevKey;
                            }
                            path.Reverse();
                            moves = path;
                            return true;
                        }

                        queue.Enqueue(next);
                    }
                }
            }

            return false;
        }

        private bool IsSolved(List<List<string>> flasks, int capacity)
        {
            foreach (var f in flasks)
            {
                if (f.Count == 0) continue;
                if (f.Count != capacity) return false;
                for (int i = 1; i < f.Count; i++)
                {
                    if (f[i] != f[0]) return false;
                }
            }
            return true;
        }

        private List<List<string>> Clone(List<List<string>> source)
        {
            var result = new List<List<string>>(source.Count);
            foreach (var f in source)
                result.Add(new List<string>(f));
            return result;
        }
    }
}
