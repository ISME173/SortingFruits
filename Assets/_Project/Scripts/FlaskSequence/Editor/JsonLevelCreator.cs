using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;
using Newtonsoft.Json;
using System.Diagnostics;

using Debug = UnityEngine.Debug;

namespace _Project.Scripts.FlaskSequence.Editor
{
    public class JsonLevelCreator : EditorWindow
    {
        private const string PrefKey_LastFolder = "JsonLevelCreator.LastFolder";
        private const string PrefKey_LevelCount = "JsonLevelCreator.LevelCount";
        private const string PrefKey_SettingsGuid = "JsonLevelCreator.SettingsGuid";
        private const string PrefKey_Cleanup = "JsonLevelCreator.CleanupExtraFiles";

        private LevelGenerationSettings _settings;
        private string _saveFolderAbsolute;
        private int _levelsToGenerate = 10;
        private bool _clearAddressableKeysBeforeGen = true;
        private bool _deleteExtraFiles = true; // новое поле

        private Vector2 _scroll;

        private bool _showPreview;
        private GUIStyle _headerStyle;
        private GUIStyle _boxStyle;
        private GUIStyle _footerStyle;
        private string _lastStatus;
        private double _lastGenerateTime;

        private LevelData _previewLevel;

        [MenuItem("Tools/Level JSON Generator")]
        public static void Open()
        {
            var window = GetWindow<JsonLevelCreator>("JSON Level Generator");
            window.minSize = new Vector2(560, 500);
        }

        private void OnEnable()
        {
            _saveFolderAbsolute = EditorPrefs.GetString(PrefKey_LastFolder, string.Empty);
            _levelsToGenerate = EditorPrefs.GetInt(PrefKey_LevelCount, 10);
            _deleteExtraFiles = EditorPrefs.GetBool(PrefKey_Cleanup, true);
            _lastStatus = "Ожидание...";

#if UNITY_EDITOR
            string guid = EditorPrefs.GetString(PrefKey_SettingsGuid, string.Empty);
            if (!string.IsNullOrEmpty(guid))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!string.IsNullOrEmpty(path))
                {
                    _settings = AssetDatabase.LoadAssetAtPath<LevelGenerationSettings>(path);
                }
            }
#endif
        }

        private void InitStyles()
        {
            if (_headerStyle != null) return;

            _headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleLeft
            };
            _boxStyle = new GUIStyle("HelpBox")
            {
                padding = new RectOffset(10, 10, 8, 8)
            };
            _footerStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleRight,
                fontSize = 10
            };
        }

        private void OnGUI()
        {
            InitStyles();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Генератор JSON уровней", _headerStyle);
            DrawSeparator();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            // Settings asset
            EditorGUILayout.BeginVertical(_boxStyle);
            EditorGUILayout.LabelField("Asset настроек", EditorStyles.boldLabel);
            var newSettings = (LevelGenerationSettings)EditorGUILayout.ObjectField(_settings, typeof(LevelGenerationSettings), false);
            if (newSettings != _settings)
            {
                _settings = newSettings;
                PersistSettingsGuid();
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            // Generation params
            EditorGUILayout.BeginVertical(_boxStyle);
            EditorGUILayout.LabelField("Параметры генерации", EditorStyles.boldLabel);
            _levelsToGenerate = EditorGUILayout.IntSlider("Количество уровней", _levelsToGenerate, 1, 5000);
            EditorPrefs.SetInt(PrefKey_LevelCount, _levelsToGenerate);

            _clearAddressableKeysBeforeGen = EditorGUILayout.ToggleLeft("Очистить список Addressables ключей перед генерацией", _clearAddressableKeysBeforeGen);
            _deleteExtraFiles = EditorGUILayout.ToggleLeft("Удалять лишние JSON файлы в папке сохранения", _deleteExtraFiles);
            EditorPrefs.SetBool(PrefKey_Cleanup, _deleteExtraFiles);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Папка сохранения (рекомендуется внутри Assets для Addressables)", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.TextField(_saveFolderAbsolute);
            EditorGUI.EndDisabledGroup();
            if (GUILayout.Button("Выбрать...", GUILayout.Width(100)))
            {
                string path = EditorUtility.OpenFolderPanel("Папка сохранения JSON", string.IsNullOrEmpty(_saveFolderAbsolute) ? Application.dataPath : _saveFolderAbsolute, "");
                if (!string.IsNullOrEmpty(path))
                {
                    _saveFolderAbsolute = path;
                    EditorPrefs.SetString(PrefKey_LastFolder, _saveFolderAbsolute);
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            var errors = ValidateInput();
            if (errors.Count > 0)
            {
                EditorGUILayout.BeginVertical(_boxStyle);
                EditorGUILayout.LabelField("Ошибки", EditorStyles.boldLabel);
                foreach (var err in errors)
                    EditorGUILayout.HelpBox(err, MessageType.Error);
                EditorGUILayout.EndVertical();
            }
            else
            {
                EditorGUILayout.HelpBox("Готово к генерации.", MessageType.Info);
            }

            EditorGUILayout.Space();
            DrawPreviewSection(errors.Count == 0);
            EditorGUILayout.Space();

            using (new EditorGUI.DisabledScope(errors.Count > 0))
            {
                if (GUILayout.Button("Сгенерировать уровни", GUILayout.Height(32)))
                    GenerateLevels();
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();
            DrawSeparator();
            EditorGUILayout.LabelField($"Статус: {_lastStatus}", EditorStyles.miniLabel);
            if (_lastGenerateTime > 0)
                EditorGUILayout.LabelField($"Время генерации: {_lastGenerateTime:F2} c.", _footerStyle);
        }

        private void PersistSettingsGuid()
        {
#if UNITY_EDITOR
            if (_settings == null)
            {
                EditorPrefs.DeleteKey(PrefKey_SettingsGuid);
                return;
            }
            string path = AssetDatabase.GetAssetPath(_settings);
            if (!string.IsNullOrEmpty(path))
            {
                string guid = AssetDatabase.AssetPathToGUID(path);
                EditorPrefs.SetString(PrefKey_SettingsGuid, guid);
            }
#endif
        }

        private void DrawPreviewSection(bool canPreview)
        {
            _showPreview = EditorGUILayout.Foldout(_showPreview, "Предпросмотр (один случайный уровень)");
            if (!_showPreview) return;

            EditorGUILayout.BeginVertical(_boxStyle);
            if (!canPreview)
            {
                EditorGUILayout.HelpBox("Нет предпросмотра — ошибки настроек.", MessageType.Warning);
                EditorGUILayout.EndVertical();
                return;
            }

            if (_previewLevel == null && GUILayout.Button("Сгенерировать предпросмотр"))
                _previewLevel = InternalGenerate(1);

            if (_previewLevel != null)
            {
                EditorGUILayout.LabelField($"LevelIndex: {_previewLevel.LevelIndex}");
                EditorGUILayout.LabelField($"FlaskCapacity: {_previewLevel.FlaskCapacity}");
                EditorGUILayout.Space();

                for (int i = 0; i < _previewLevel.Flasks.Count; i++)
                {
                    var flask = _previewLevel.Flasks[i];
                    string line = flask.Count == 0 ? "[EMPTY]" : string.Join(",", flask);
                    EditorGUILayout.LabelField($"Колба {i + 1}: {line}");
                }

                if (GUILayout.Button("Обновить предпросмотр"))
                    _previewLevel = InternalGenerate(1);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawSeparator()
        {
            var rect = EditorGUILayout.GetControlRect(false, 2);
            EditorGUI.DrawRect(rect, new Color(0.25f, 0.25f, 0.25f, 1f));
        }

        private List<string> ValidateInput()
        {
            var errors = new List<string>();

            if (_settings == null)
                errors.Add("Не указан LevelGenerationSettings asset.");

            if (string.IsNullOrEmpty(_saveFolderAbsolute))
                errors.Add("Не выбрана папка сохранения.");

            if (!string.IsNullOrEmpty(_saveFolderAbsolute) && !Directory.Exists(_saveFolderAbsolute))
                errors.Add("Указанная папка не существует.");

            if (_settings != null)
            {
                if (_settings.AvailableFruitNames == null || _settings.AvailableFruitNames.Count < _settings.MinFruitTypes)
                    errors.Add("Список фруктов меньше минимального количества типов.");
                if (_settings.MinFruitTypes > _settings.MaxFruitTypes)
                    errors.Add("MinFruitTypes > MaxFruitTypes.");
                if (_settings.MinExtraEmptyFlasks > _settings.MaxExtraEmptyFlasks)
                    errors.Add("MinExtraEmptyFlasks > MaxExtraEmptyFlasks.");
                if (_settings.FlaskCapacity <= 0)
                    errors.Add("FlaskCapacity должен быть > 0.");
                if (_settings.MaxFlasksPerLevel < (_settings.MinFruitTypes + 1))
                    errors.Add("MaxFlasksPerLevel меньше минимально необходимого (MinFruitTypes + обязательная пустая).");
            }

            return errors;
        }

        private void GenerateLevels()
        {
            if (_settings == null) return;

            double startTime = EditorApplication.timeSinceStartup;
            int generated = 0;
            var generatedKeysLocal = new List<string>();

            try
            {
                if (_clearAddressableKeysBeforeGen)
                    _settings.GeneratedLevelKeys.Clear();

                _settings.LastGenerationTimestamp = DateTime.UtcNow.Ticks;
                _settings.ForceReloadOnNextPlay = true;

                bool canceled = false;
                int total = _levelsToGenerate;
                for (int levelIndex = 1; levelIndex <= total; levelIndex++)
                {
                    if (EditorUtility.DisplayCancelableProgressBar(
                        "Генерация уровней",
                        $"Создание уровня {levelIndex}/{total}",
                        (float)levelIndex / total))
                    {
                        canceled = true;
                        break;
                    }

                    LevelData data = TryGenerateLevel(levelIndex);
                    if (data == null)
                    {
                        Debug.LogError($"[LevelGen] Не удалось создать валидный уровень {levelIndex}");
                        continue;
                    }

                    data.LevelState = LevelState.Locked;

                    string fileName = $"Level_{data.LevelIndex}.json";
                    string key = Path.GetFileNameWithoutExtension(fileName);
                    string fullPath = Path.Combine(_saveFolderAbsolute, fileName);

                    string json = JsonConvert.SerializeObject(data, Formatting.Indented);
                    File.WriteAllText(fullPath, json);
                    generated++;

                    generatedKeysLocal.Add(key);

                    if (!_settings.GeneratedLevelKeys.Contains(key))
                        _settings.GeneratedLevelKeys.Add(key);
                }

                EditorUtility.ClearProgressBar();

                if (!canceled && _deleteExtraFiles && Directory.Exists(_saveFolderAbsolute))
                {
                    var expectedFiles = new HashSet<string>(
                        generatedKeysLocal.Select(k => k + ".json"),
                        StringComparer.OrdinalIgnoreCase);

                    var allJson = Directory.GetFiles(_saveFolderAbsolute, "*.json", SearchOption.TopDirectoryOnly);
                    int deleted = 0;
                    foreach (var path in allJson)
                    {
                        string name = Path.GetFileName(path);
                        if (!expectedFiles.Contains(name))
                        {
                            File.Delete(path);
                            deleted++;
                        }
                    }
                    if (deleted > 0)
                        Debug.Log($"[LevelGen] Удалено лишних JSON файлов: {deleted}");
                }

                EditorUtility.SetDirty(_settings);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                _lastStatus = canceled
                    ? $"Отменено пользователем. Сгенерировано {generated} уровней. Ключей: {_settings.GeneratedLevelKeys.Count}"
                    : $"Сгенерировано {generated} уровней. Ключей: {_settings.GeneratedLevelKeys.Count}";
                Debug.Log($"[LevelGen] {(canceled ? "Отмена." : "Готово.")} Сгенерировано {generated} уровней. Папка: {_saveFolderAbsolute}");
            }
            catch (Exception ex)
            {
                EditorUtility.ClearProgressBar();
                _lastStatus = "Ошибка генерации.";
                Debug.LogError("[LevelGen] Ошибка генерации: " + ex);
            }

            _lastGenerateTime = EditorApplication.timeSinceStartup - startTime;
        }

        private LevelData TryGenerateLevel(int levelIndex)
        {
            if (_settings == null)
                return null;

            // Удалён лимит по времени. Остался лимит по попыткам.
            for (int attempt = 1; attempt <= _settings.MaxGenerationAttemptsPerLevel; attempt++)
            {
                LevelData candidate = InternalGenerate(levelIndex);
                if (candidate == null) continue;
                if (IsSolved(candidate) || (_settings.AvoidAlmostSolved && IsAlmostSolved(candidate)))
                    continue;

                if (_settings.UseSolverValidation)
                {
                    bool solved = Solve(candidate, out int depth);
                    if (!solved) continue;
                    if (depth < _settings.MinSolutionMoves) continue;
                }

                return candidate;
            }
            return null;
        }

        private LevelData InternalGenerate(int levelIndex)
        {
            // 1) Рассчитываем «эффективный» уровень сложности с учетом капа и дрожания
            int effectiveLevelIndex = levelIndex;
            int cap = Mathf.Max(1, _settings.DifficultyCapLevel);
            if (effectiveLevelIndex > cap)
            {
                int jitter = Mathf.Clamp(_settings.PostCapJitterRange, 0, 10000);
                if (jitter > 0)
                {
                    int delta = Random.Range(-jitter, jitter + 1);
                    effectiveLevelIndex = Mathf.Clamp(cap + delta, 1, int.MaxValue);
                }
                else
                {
                    effectiveLevelIndex = cap;
                }
            }

            // 2) Применяем рост параметров по effectiveLevelIndex
            int fruitTypes = Mathf.Clamp(
                _settings.MinFruitTypes + (effectiveLevelIndex - 1) / _settings.LevelsPerFruitIncrease,
                _settings.MinFruitTypes,
                _settings.MaxFruitTypes);

            int extraEmpty = Mathf.Clamp(
                _settings.MinExtraEmptyFlasks + (effectiveLevelIndex - 1) / _settings.LevelsPerExtraEmptyFlaskIncrease,
                _settings.MinExtraEmptyFlasks,
                _settings.MaxExtraEmptyFlasks);

            int capacity = _settings.FlaskCapacity;

            // 3) Ограничения по общему числу колб
            int maxFlasks = _settings.MaxFlasksPerLevel;
            int baseNeeded = fruitTypes + 1; // обязательная пустая
            if (baseNeeded > maxFlasks)
            {
                fruitTypes = Mathf.Clamp(maxFlasks - 1, _settings.MinFruitTypes, fruitTypes);
                baseNeeded = fruitTypes + 1;
            }
            if (baseNeeded + extraEmpty > maxFlasks)
                extraEmpty = Mathf.Max(0, maxFlasks - baseNeeded);

            int totalFlasks = fruitTypes + 1 + extraEmpty;

            // 4) Формируем пул фруктов
            var fruits = _settings.AvailableFruitNames.Take(fruitTypes).ToList();
            int filledFlasksCount = fruitTypes;
            int emptyFlasksCount = totalFlasks - filledFlasksCount;

            var pool = new List<string>(fruitTypes * capacity);
            foreach (var f in fruits)
                for (int i = 0; i < capacity; i++)
                    pool.Add(f);

            Shuffle(pool);

            var flasks = new List<List<string>>(totalFlasks);
            for (int i = 0; i < filledFlasksCount; i++)
                flasks.Add(new List<string>(capacity));
            for (int i = 0; i < emptyFlasksCount; i++)
                flasks.Add(new List<string>(capacity));

            foreach (var fruit in pool)
            {
                var candidateIndexes = Enumerable.Range(0, filledFlasksCount)
                    .Where(idx => flasks[idx].Count < capacity).ToList();
                if (candidateIndexes.Count == 0)
                    break;

                int chosen = candidateIndexes[Random.Range(0, candidateIndexes.Count)];
                flasks[chosen].Add(fruit);
            }

            if (_settings.BreakSolvedFlasks)
                BreakFullySolvedFlasksRandom(flasks, capacity);

            var data = new LevelData
            {
                LevelIndex = levelIndex, // сохраняем реальный индекс уровня
                FlaskCapacity = capacity,
                Flasks = flasks
            };

            // 5) Фильтрация слишком простых конфигураций
            if (IsSolved(data) || (_settings.AvoidAlmostSolved && IsAlmostSolved(data)))
                return null;

            return data;
        }

        private void BreakFullySolvedFlasksRandom(List<List<string>> flasks, int capacity)
        {
            var emptyIndices = flasks
                .Select((f, i) => new { f, i })
                .Where(x => x.f.Count == 0)
                .Select(x => x.i)
                .ToList();

            bool hasEmpty = emptyIndices.Count > 0;

            for (int i = 0; i < flasks.Count; i++)
            {
                var flask = flasks[i];
                if (flask.Count != capacity) continue;

                bool allSame = flask.All(x => x == flask[0]);
                if (!allSame) continue;

                if (hasEmpty)
                {
                    int emptyIdx = emptyIndices[Random.Range(0, emptyIndices.Count)];
                    int moveCount = Random.Range(1, Math.Min(2, flask.Count) + 1);
                    for (int m = 0; m < moveCount; m++)
                    {
                        string val = flask[flask.Count - 1];
                        flask.RemoveAt(flask.Count - 1);
                        flasks[emptyIdx].Add(val);
                    }
                }
                else
                {
                    int target = -1;
                    for (int t = 0; t < flasks.Count; t++)
                    {
                        if (t == i) continue;
                        if (flasks[t].Count == 0) continue;
                        bool allSameTarget = flasks[t].All(x => x == flasks[t][0]);
                        if (!allSameTarget || flasks[t][0] != flask[0])
                        {
                            target = t;
                            break;
                        }
                    }

                    if (target != -1)
                    {
                        string a = flask[flask.Count - 1];
                        string b = flasks[target][flasks[target].Count - 1];
                        flask[flask.Count - 1] = b;
                        flasks[target][flasks[target].Count - 1] = a;
                    }
                }
            }
        }

        private static void Shuffle<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        private bool IsSolved(LevelData level)
        {
            int cap = level.FlaskCapacity;
            foreach (var flask in level.Flasks)
            {
                if (flask.Count == 0) continue;
                if (flask.Count != cap) return false;
                for (int i = 1; i < flask.Count; i++)
                    if (flask[i] != flask[0]) return false;
            }
            return true;
        }

        private bool IsAlmostSolved(LevelData level)
        {
            int cap = level.FlaskCapacity;
            int unsolved = 0;
            foreach (var flask in level.Flasks)
            {
                if (flask.Count == 0) continue;
                if (flask.Count == cap)
                {
                    for (int i = 1; i < flask.Count; i++)
                        if (flask[i] != flask[0]) { unsolved++; break; }
                }
                else
                {
                    unsolved++;
                }
            }
            return unsolved <= 1;
        }

        private static Dictionary<string, string> BuildFruitCodeMap(List<List<string>> flasks)
        {
            var unique = new HashSet<string>();
            foreach (var f in flasks)
                foreach (var x in f)
                    unique.Add(x);

            var list = unique.ToList();
            var map = new Dictionary<string, string>(list.Count);
            const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            for (int i = 0; i < list.Count; i++)
            {
                if (i < alphabet.Length)
                    map[list[i]] = alphabet[i].ToString();
                else
                {
                    int a = (i / alphabet.Length) % alphabet.Length;
                    int b = i % alphabet.Length;
                    map[list[i]] = new string(new[] { alphabet[a], alphabet[b] });
                }
            }
            return map;
        }

        private bool Solve(LevelData startLevel, out int solvedDepth)
        {
            solvedDepth = -1;
            int capacity = startLevel.FlaskCapacity;
            if (IsSolved(startLevel))
            {
                solvedDepth = 0;
                return true;
            }

            int maxStates = Mathf.Max(10000, _settings.MaxSolverStates);
            int maxDepth = Math.Max(_settings.MinSolutionMoves * 10, 50);

            var startState = Clone(startLevel.Flasks);
            var fruitCode = BuildFruitCodeMap(startState);
            string startKey = EncodeFast(startState, fruitCode);

            var visited = new HashSet<string> { startKey };
            var queue = new Queue<(List<List<string>> state, int depth)>();
            queue.Enqueue((startState, 0));

            int processed = 0;
            while (queue.Count > 0)
            {
                if (processed > maxStates) return false;

                var (state, depth) = queue.Dequeue();
                processed++;
                if (depth >= maxDepth) continue;

                for (int fromIdx = 0; fromIdx < state.Count; fromIdx++)
                {
                    var from = state[fromIdx];
                    if (from.Count == 0) continue;

                    string topFruit = from[^1];
                    int groupSize = 1;
                    for (int i = from.Count - 2; i >= 0; i--)
                    {
                        if (from[i] == topFruit) groupSize++;
                        else break;
                    }

                    bool fromIsMonoFull = from.Count == capacity && from.All(x => x == topFruit);

                    for (int toIdx = 0; toIdx < state.Count; toIdx++)
                    {
                        if (toIdx == fromIdx) continue;
                        var to = state[toIdx];
                        if (to.Count >= capacity) continue;
                        if (to.Count > 0 && to[^1] != topFruit) continue;

                        bool toIsMono = to.Count > 0 && to.All(x => x == to[0]);

                        if (fromIsMonoFull && toIsMono && to.Count + groupSize <= capacity && to.Count > 0 && to[0] == topFruit)
                            continue;

                        int freeSlots = capacity - to.Count;
                        int moveCount = Math.Min(groupSize, freeSlots);
                        if (moveCount <= 0) continue;

                        var next = Clone(state);
                        var nFrom = next[fromIdx];
                        var nTo = next[toIdx];
                        for (int m = 0; m < moveCount; m++)
                        {
                            string val = nFrom[^1];
                            nFrom.RemoveAt(nFrom.Count - 1);
                            nTo.Add(val);
                        }

                        string key = EncodeFast(next, fruitCode);
                        if (visited.Contains(key)) continue;
                        visited.Add(key);

                        if (IsSolvedQuick(next, capacity))
                        {
                            solvedDepth = depth + 1;
                            return true;
                        }

                        queue.Enqueue((next, depth + 1));
                    }
                }
            }

            return false;
        }

        private static bool IsSolvedQuick(List<List<string>> flasks, int capacity)
        {
            foreach (var flask in flasks)
            {
                if (flask.Count == 0) continue;
                if (flask.Count != capacity) return false;
                for (int i = 1; i < flask.Count; i++)
                    if (flask[i] != flask[0]) return false;
            }
            return true;
        }

        private static string EncodeFast(List<List<string>> state, Dictionary<string, string> map)
        {
            var sb = new StringBuilder(state.Count * 8);
            for (int i = 0; i < state.Count; i++)
            {
                if (i > 0) sb.Append('|');
                var flask = state[i];
                for (int j = 0; j < flask.Count; j++)
                {
                    if (j > 0) sb.Append(',');
                    sb.Append(map[flask[j]]);
                }
            }
            return sb.ToString();
        }

        private static string Encode(List<List<string>> state)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < state.Count; i++)
            {
                if (i > 0) sb.Append('|');
                var flask = state[i];
                for (int j = 0; j < flask.Count; j++)
                {
                    if (j > 0) sb.Append(',');
                    sb.Append(flask[j]);
                }
            }
            return sb.ToString();
        }

        private static List<List<string>> Clone(List<List<string>> source)
        {
            var result = new List<List<string>>(source.Count);
            foreach (var f in source)
                result.Add(new List<string>(f));
            return result;
        }
    }
}
