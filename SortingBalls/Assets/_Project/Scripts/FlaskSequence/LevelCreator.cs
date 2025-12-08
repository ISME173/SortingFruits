using _Project.Scripts.Audio;
using _Project.Scripts.GameEvents;
using _Project.Scripts.Saves;
using Newtonsoft.Json;
using Reflex.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace _Project.Scripts.FlaskSequence
{
    public class LevelCreator : MonoBehaviour
    {
        public readonly List<Flask> SpawnedFlasks = new List<Flask>();

        private readonly List<LevelData> AllLevels = new List<LevelData>();
        private readonly List<string> _loadedLevelKeys = new List<string>();

        private const string SaveKey_CurrentLevel = "FlaskSequence_LastPlayedLevelKey";

        [Header("References")]
        [SerializeField] private Flask _flaskPrefab;
        [SerializeField] private Transform _startCreateFlasksPoint;
        [Space]
        [SerializeField] private List<Item> _itemPrefabs;

        [Header("Settings")]
        [SerializeField, Min(0)] private float _spawnOffsetBetweenFlasks;
        [SerializeField, Min(0)] private float _spawnRowOffsetY = 2f;
        [SerializeField, Min(1)] private int _flasksCountInRow = 4;
        [Space]
        [SerializeField] private bool _saveInOnDestroy = true;

        [Header("Assets")]
        [SerializeField] private LevelGenerationSettings _generationSettings;

        private FlaskItemsMover _flaskItemsMover;
        private ISaves _saves;
        private IAudioService _audioService;
        private IGameEvents _gameEvents;
        private int _currentLevelIndex = -1;
        private bool _allLevelsLoaded = false;
        private bool _forceReloadGeneration;
        private int _currentSpawnFlasksRow = 1;
        private List<string> _orderedGeneratedKeys = null;

        public event Action<LevelData> LevelCreated, LevelCompleted, LevelLoaded;
        public event Action<int, LevelState> OnLevelStateChanged;

        public int CurrentLevelIndex => _currentLevelIndex;
        public int LevelsCount => _generationSettings.GeneratedLevelKeys.Count;
        public int LoadedLevelsCount => AllLevels.Count;
        public int OpenedLevelsCount => AllLevels.Where(level => level.LevelState == LevelState.Opened).Count();
        public int CompletedLevelsCount => AllLevels.Where(level => level.LevelState == LevelState.Completed).Count();

        private async void Awake()
        {
            _forceReloadGeneration = _generationSettings != null && _generationSettings.ForceReloadOnNextPlay;

            BuildOrderedGeneratedKeys();

            await LoadFirstLevelAndCreateView();
            _ = LoadRemainingLevels();

            // После первой загрузки сбрасываем флаг, чтобы в следующий запуск использовать сохранения
            if (_generationSettings != null && _generationSettings.ForceReloadOnNextPlay)
            {
                _generationSettings.ForceReloadOnNextPlay = false;
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(_generationSettings);
#endif
            }
        }

        private void OnDestroy()
        {
            if (_saveInOnDestroy)
            {
                SaveLevels();
                SaveCurrentLevelKey();
            }
        }

        private void SaveLevels()
        {
            // Экономная сериализация — сохраняем только текущий открытый уровень.
            if (_saves == null)
                return;

            if (_currentLevelIndex < 0 || _currentLevelIndex >= _loadedLevelKeys.Count || _currentLevelIndex >= AllLevels.Count)
                return;

            string currentKey = _loadedLevelKeys[_currentLevelIndex];
            LevelData currentLevel = AllLevels[_currentLevelIndex];

            try
            {
                // Удаляем все прочие сохранённые уровни (используем SetObject<LevelData>(key, null) — реализация PlayerPrefsSaves удалит ключ).
                if (_orderedGeneratedKeys != null)
                {
                    foreach (var key in _orderedGeneratedKeys)
                    {
                        if (string.IsNullOrEmpty(key))
                            continue;
                        if (key == currentKey)
                            continue;

                        try
                        {
                            _saves.SetObject<LevelData>(key, null, prettyPrint: false);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning($"[LevelCreator] Не удалось удалить старую запись уровня '{key}' из ISaves: {ex.Message}");
                        }
                    }
                }

                // Сохраняем только текущий уровень
                if (!string.IsNullOrEmpty(currentKey) && currentLevel != null)
                {
                    try
                    {
                        _saves.SetObject(currentKey, currentLevel, prettyPrint: true);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[LevelCreator] Не удалось сохранить текущий уровень '{currentKey}' в ISaves: {ex.Message}");
                    }
                }

                _saves.Save();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[LevelCreator] Ошибка при сохранении уровней: {ex.Message}");
            }
        }

        #region Загрузка уровней

        private void BuildOrderedGeneratedKeys()
        {
            if (_generationSettings == null || _generationSettings.GeneratedLevelKeys == null)
            {
                _orderedGeneratedKeys = new List<string>();
                return;
            }

            // Копируем оригинальный список
            var original = _generationSettings.GeneratedLevelKeys;
            _orderedGeneratedKeys = new List<string>(original);

            if (_saves == null)
                return;

            if (_forceReloadGeneration)
                return;

            try
            {
                if (_saves.HasKey(SaveKey_CurrentLevel))
                {
                    string savedKey = _saves.GetString(SaveKey_CurrentLevel);
                    if (!string.IsNullOrEmpty(savedKey) && original.Contains(savedKey))
                    {
                        _orderedGeneratedKeys.Remove(savedKey);
                        _orderedGeneratedKeys.Insert(0, savedKey);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[LevelCreator] Ошибка чтения сохранённого ключа уровня: {ex.Message}");
            }
        }

        private async Task LoadFirstLevelAndCreateView()
        {
            if (_generationSettings == null || _orderedGeneratedKeys == null || _orderedGeneratedKeys.Count == 0)
            {
                Debug.LogWarning("[LevelCreator] Нет настроек или списка ключей уровней.");
                return;
            }

            string firstKey = _orderedGeneratedKeys[0];

            LevelData level = await LoadLevelByKey(firstKey);
            if (level != null)
            {
                AllLevels.Add(level);
                _loadedLevelKeys.Add(firstKey);

                _currentLevelIndex = level.LevelIndex - 1;

                SetLevelStateByIndex(_currentLevelIndex, LevelState.Opened);
                CreateLevelView(level);
                SaveCurrentLevelKey();

                _gameEvents.GameReadyApi();
            }
            else
            {
                Debug.LogError($"[LevelCreator] Не удалось загрузить первый уровень по ключу '{firstKey}'.");
            }
        }

        private async Task LoadRemainingLevels()
        {
            if (_orderedGeneratedKeys == null)
                return;

            // Оригинальный список для определения порядковых индексов в Addressables
            var originalKeys = _generationSettings?.GeneratedLevelKeys;

            int origCurrentIndex = -1;
            if (originalKeys != null && _loadedLevelKeys.Count > 0)
            {
                // Попытаемся определить индекс текущего ключа в оригинальном списке
                var currentKey = _loadedLevelKeys[0]; // первый загруженный ключ соответствует началу _orderedGeneratedKeys
                origCurrentIndex = originalKeys.IndexOf(currentKey);
            }

            for (int i = 1; i < _orderedGeneratedKeys.Count; i++)
            {
                string key = _orderedGeneratedKeys[i];

                if (originalKeys != null && origCurrentIndex >= 0)
                {
                    int origIdx = originalKeys.IndexOf(key);
                    if (origIdx >= 0 && origIdx < origCurrentIndex)
                    {
                        // Этот ключ расположен в оригинальном списке до текущего — считаем уровень пройденным и не грузим его

                        OnLevelStateChanged?.Invoke(origIdx, LevelState.Completed);
                    }
                    else
                    {
                        OnLevelStateChanged?.Invoke(origIdx, LevelState.Locked);
                    }
                }
            }

            for (int i = 1; i < _orderedGeneratedKeys.Count; i++)
            {
                string key = _orderedGeneratedKeys[i];

                // Попробуем пропустить явно пройденные уровни, если можем определить их по позиции в оригинальном списке
                bool skipAsPassed = false;
                if (originalKeys != null && origCurrentIndex >= 0)
                {
                    int origIdx = originalKeys.IndexOf(key);
                    if (origIdx >= 0 && origIdx < origCurrentIndex)
                    {
                        skipAsPassed = true;
                    }
                }

                if (skipAsPassed)
                {
                    // Не загружаем контент пройденного уровня, чтобы экономить память/записи.
                    // Просто пометим, что ключ "загружен" (вспомогательная коллекция) — это нужно для корректной работы сохранения текущего уровня.
                    _loadedLevelKeys.Add(key);
                    continue;
                }


                LevelData level = await LoadLevelByKey(key);

                if (level != null)
                {
                    AllLevels.Add(level);
                    _loadedLevelKeys.Add(key);

                    LevelLoaded?.Invoke(level);
                }
            }

            // Сохраняем один раз после массовой загрузки (если нужно)
            if (_saves != null)
                _saves.Save();

            _allLevelsLoaded = true;
            Debug.Log($"[LevelCreator] Все уровни загружены. Всего: {AllLevels.Count}");
        }

        private async Task<LevelData> LoadLevelByKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return null;

            // Если требуется принудительная перезагрузка – игнорируем сохранения.
            if (!_forceReloadGeneration && _saves != null && _saves.HasKey(key))
            {
                LevelData saved = _saves.GetObject<LevelData>(key, default);
                if (saved != null)
                    return saved;

                string jsonStr = _saves.GetString(key, string.Empty);
                if (!string.IsNullOrEmpty(jsonStr))
                {
                    try
                    {
                        LevelData parsed = JsonConvert.DeserializeObject<LevelData>(jsonStr);
                        if (parsed != null)
                            return parsed;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[LevelCreator] Ошибка чтения сохранённого JSON для '{key}': {ex.Message}");
                    }
                }
            }

            // Грузим из Addressables (всегда при принудительной перезагрузке или если нет сохранения)
            AsyncOperationHandle<TextAsset> handle = Addressables.LoadAssetAsync<TextAsset>(key);
            await handle.Task;

            LevelData result = null;
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                try
                {
                    var json = handle.Result.text;
                    result = JsonConvert.DeserializeObject<LevelData>(json);
                    if (result == null)
                        Debug.LogError($"[LevelCreator] Json пустой или неверный для ключа '{key}'.");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[LevelCreator] Ошибка парсинга JSON для '{key}': {ex.Message}");
                }
            }
            else
            {
                Debug.LogError($"[LevelCreator] Не удалось загрузить Addressable по ключу '{key}'.");
            }

            Addressables.Release(handle);

            // Раньше тут для каждого загруженного уровня делалась запись в ISaves — это приводило к множеству операций записи.
            // Теперь мы НЕ сохраняем все загруженные уровни автоматически. Сохраняем только текущий открытый уровень через SaveLevels / SaveLevelKey.
            return result;
        }

        #endregion

        #region Управление уровнями

        private void SetLevelStateByIndex(int levelIndex, LevelState levelState)
        {
            LevelData levelData = AllLevels.FirstOrDefault(x => x.LevelIndex - 1 == levelIndex);

            if (levelData == null)
            {
                Debug.LogWarning($"Invalid level key: {levelIndex}");
                return;
            }

            Debug.Log($"Set level {levelIndex} state to {levelState}");


            levelData.LevelState = levelState;
            OnLevelStateChanged?.Invoke(levelIndex, levelState);
        }

        [ContextMenu("LevelsControll/LoadNextLevel")]
        public void LoadNextLevel()
        {
            int nextIndex = _currentLevelIndex + 1;
            if (nextIndex >= AllLevels.Count)
            {
                Debug.LogWarning(!_allLevelsLoaded
                    ? "[LevelCreator] Следующий уровень ещё не загружен."
                    : "[LevelCreator] Нет следующего уровня.");
                return;
            }

            _currentLevelIndex = nextIndex;
            SetLevelStateByIndex(_currentLevelIndex, LevelState.Opened);

            CreateLevelView(AllLevels.Find(x => x.LevelIndex - 1 == _currentLevelIndex));

            // Сохраняем выбранный текущий уровень
            SaveCurrentLevelKey();
        }

        [ContextMenu("LevelsControll/ReloadCurrentLevel")]
        public void ReloadCurrentLevel()
        {
            if (_currentLevelIndex < 0 || _currentLevelIndex >= AllLevels.Count)
            {
                Debug.LogWarning("[LevelCreator] Текущий индекс уровня некорректен.");
                return;
            }
            CreateLevelView(AllLevels.Find(x => x.LevelIndex - 1 == _currentLevelIndex));
        }

        public void LoadLevelByIndex(int index)
        {
            if (index < 0 || index >= AllLevels.Count)
            {
                Debug.LogWarning($"[LevelCreator] Индекс {index} вне диапазона загруженных уровней.");
                return;
            }

            _currentLevelIndex = index;

            CreateLevelView(AllLevels[_currentLevelIndex]);

            // Сохраняем выбранный текущий уровень
            SaveCurrentLevelKey();
        }

        #endregion

        public void AddEmptyFlask()
        {
            // Ограничения и проверки
            if (!CanCreateFlask())
            {
                Debug.LogWarning("[LevelCreator] Превышен лимит колб для уровня.");
                return;
            }

            if (_flaskPrefab == null || _startCreateFlasksPoint == null)
            {
                Debug.LogError("[LevelCreator] Не назначены ссылки на префаб колбы или стартовую точку.");
                return;
            }

            // Базовые параметры позиционирования
            Vector3 basePos = _startCreateFlasksPoint.position;
            float spacingX = _spawnOffsetBetweenFlasks;
            float spacingY = _spawnRowOffsetY;

            int totalBeforeAdd = SpawnedFlasks.Count;

            // Собираем существующие ряды по Y (с допуском по высоте)
            const float yTolerance = 0.001f;
            // rowIndex -> индексы колб в SpawnedFlasks
            Dictionary<int, List<int>> rows = new Dictionary<int, List<int>>();
            for (int i = 0; i < SpawnedFlasks.Count; i++)
            {
                Flask f = SpawnedFlasks[i];
                if (f == null) continue;
                float y = f.transform.position.y;

                // Вычисляем ближайший rowIndex по сетке spacingY
                int rowIndex = Mathf.RoundToInt((y - basePos.y) / spacingY);
                float expectedY = basePos.y + rowIndex * spacingY;
                if (Mathf.Abs(y - expectedY) > yTolerance)
                {
                    // Если колба "между" — приведём к ближайшему ряду
                    rowIndex = Mathf.RoundToInt((y - basePos.y) / spacingY);
                }

                if (!rows.TryGetValue(rowIndex, out var list))
                {
                    list = new List<int>();
                    rows[rowIndex] = list;
                }
                list.Add(i);
            }

            // Убедимся, что два базовых ряда существуют в словаре (даже если пустые)
            if (!rows.ContainsKey(0)) rows[0] = new List<int>();
            if (!rows.ContainsKey(1)) rows[1] = new List<int>();

            int row0Count = rows[0].Count;
            int row1Count = rows[1].Count;

            // Определяем желаемый ряд по чередованию попарно: 2 в 0, 2 в 1, ...
            int pairIndex = totalBeforeAdd / 2;
            int desiredRow = pairIndex % 2; // 0 или 1

            // Корректируем целевой ряд, чтобы не нарушать максимум
            int targetRow = desiredRow;
            bool desiredRowFull = (desiredRow == 0 ? row0Count : row1Count) >= _flasksCountInRow;
            if (desiredRowFull)
            {
                int otherRow = desiredRow == 0 ? 1 : 0;
                bool otherRowHasSpace = (otherRow == 0 ? row0Count : row1Count) < _flasksCountInRow;
                if (otherRowHasSpace)
                {
                    targetRow = otherRow;
                }
                else
                {
                    // Оба базовых ряда заполнены — создаём новый ряд выше
                    targetRow = rows.Keys.Count == 0 ? 0 : (rows.Keys.Max() + 1);
                    if (!rows.ContainsKey(targetRow))
                        rows[targetRow] = new List<int>();
                }
            }

            // Количество в целевом ряду до/после добавления
            int currentRowCountBeforeAdd = rows[targetRow].Count;
            int currentRowCountAfterAdd = currentRowCountBeforeAdd + 1;

            // Центрирование: стартовая X-точка для выбранного ряда после добавления
            float startXAfterAdd = basePos.x - 0.5f * spacingX * (currentRowCountAfterAdd - 1);
            float rowY = basePos.y + targetRow * spacingY;

            // 1) Перепозиционируем уже созданные колбы выбранного ряда для центрирования
            int indexInRow = 0;
            foreach (int spawnedIndex in rows[targetRow])
            {
                Flask existing = SpawnedFlasks[spawnedIndex];
                if (existing == null) continue;

                float x = startXAfterAdd + indexInRow * spacingX;
                Vector3 targetPos = new Vector3(x, rowY, basePos.z);
                existing.transform.position = targetPos;

                indexInRow++;
            }

            // 2) Создаём новую пустую колбу как последний элемент выбранного ряда
            float newX = startXAfterAdd + (currentRowCountAfterAdd - 1) * spacingX;
            Vector3 newSpawnPos = new Vector3(newX, rowY, basePos.z);

            Flask newFlask = Instantiate(_flaskPrefab, newSpawnPos, Quaternion.identity, _startCreateFlasksPoint.parent);
            newFlask.InjectAudioService(_audioService);
            SpawnedFlasks.Add(newFlask);
            newFlask.OnFilled += OnFilledFlask;

            // Обновим служебный счётчик количества задействованных рядов
            _currentSpawnFlasksRow = Math.Max(_currentSpawnFlasksRow, targetRow + 1);

            // Пустая колба — без добавления предметов
        }

        public bool CanCreateFlask()
        {
            return SpawnedFlasks.Count < _generationSettings.MaxFlasksPerLevel;
        }

        /// <summary>
        /// Создаёт визуальное представление уровня по данным <see cref="LevelData"/>.
        /// Новая логика: колбы спавнятся попарно, чередуя ряды (2 в ряд 0, 2 в ряд 1, далее снова 2 в ряд 0 и т.д.).
        /// Каждый ряд центрируется относительно _startCreateFlasksPoint。
        /// </summary>
        private void CreateLevelView(LevelData levelData)
        {
            if (levelData == null)
            {
                Debug.LogError("[LevelCreator] LevelData is null.");
                return;
            }

            if (_flaskPrefab == null || _startCreateFlasksPoint == null)
            {
                Debug.LogError("[LevelCreator] Не назначены ссылки на префаб колбы или стартовую точку.");
                return;
            }

            ClearCurrentLevelView();

            int total = levelData.Flasks.Count;
            if (total <= 0)
                return;

            Vector3 basePos = _startCreateFlasksPoint.position;
            float spacingX = _spawnOffsetBetweenFlasks;
            float spacingY = _spawnRowOffsetY;

            // Текущие счётчики по рядам
            int row0Count = 0;
            int row1Count = 0;

            for (int idx = 0; idx < total; idx++)
            {
                // Определяем ряд для текущей колбы
                int pairIndex = idx / 2;
                int row = pairIndex % 2; // 0 или 1

                // Текущее количество в этом ряду до добавления
                int currentRowCountBeforeAdd = (row == 0) ? row0Count : row1Count;
                int currentRowCountAfterAdd = currentRowCountBeforeAdd + 1;

                // Центрирование выбранного ряда с учётом добавления
                float startXAfterAdd = basePos.x - 0.5f * spacingX * (currentRowCountAfterAdd - 1);
                float y = basePos.y + row * spacingY;

                // Перепозиционируем уже созданные колбы этого ряда для центрирования
                int indexInRow = 0;
                for (int j = 0; j < SpawnedFlasks.Count; j++)
                {
                    int pj = j / 2;
                    int rj = pj % 2;
                    if (rj != row)
                        continue;

                    float xj = startXAfterAdd + indexInRow * spacingX;
                    Vector3 posj = new Vector3(xj, y, basePos.z);

                    Flask existing = SpawnedFlasks[j];
                    if (existing != null)
                        existing.transform.position = posj;

                    indexInRow++;
                }

                // Позиция новой колбы как последний элемент ряда
                float newX = startXAfterAdd + (currentRowCountAfterAdd - 1) * spacingX;
                Vector3 spawnPos = new Vector3(newX, y, basePos.z);

                // Создаём колбу
                Flask flaskInstance = Instantiate(_flaskPrefab, spawnPos, Quaternion.identity, _startCreateFlasksPoint.parent);
                flaskInstance.InjectAudioService(_audioService);
                SpawnedFlasks.Add(flaskInstance);
                flaskInstance.OnFilled += OnFilledFlask;

                // Обновляем счетчики рядов
                if (row == 0) row0Count++; else row1Count++;
                _currentSpawnFlasksRow = Math.Max(_currentSpawnFlasksRow, row + 1);

                // Наполняем колбу предметами по данным уровня
                List<string> fruitsInFlask = levelData.Flasks[idx];
                if (fruitsInFlask == null || fruitsInFlask.Count == 0)
                    continue;

                for (int j = 0; j < fruitsInFlask.Count; j++)
                {
                    string fruitName = fruitsInFlask[j];
                    Item prefab = FindItemPrefab(fruitName);
                    if (prefab == null)
                    {
                        Debug.LogWarning($"[LevelCreator] Не найден префаб фрукта '{fruitName}'. Пропуск.");
                        continue;
                    }

                    Transform slotTransform = flaskInstance.GetFirstEmptySlotTransform();
                    if (slotTransform == null)
                    {
                        Debug.LogWarning($"[LevelCreator] Нет свободного слота в колбе {idx + 1} при добавлении '{fruitName}'.");
                        break;
                    }

                    Item itemInstance = Instantiate(prefab, slotTransform.position, Quaternion.identity);
                    itemInstance.transform.SetParent(slotTransform, worldPositionStays: false);
                    itemInstance.transform.localPosition = Vector3.zero;

                    if (!flaskInstance.TryAddItem(itemInstance))
                    {
                        Debug.LogWarning($"[LevelCreator] TryAddItem вернул false для '{fruitName}' в колбе {idx + 1}.");
                        Destroy(itemInstance.gameObject);
                        break;
                    }
                }
            }

            LevelCreated?.Invoke(levelData);
        }

        private void OnFilledFlask()
        {
            if (SpawnedFlasks.All(flask => flask.IsFilled || flask.IsEmpty))
            {
                Debug.Log($"Level {_currentLevelIndex + 1} completed!");
                if (_flaskItemsMover.IsMovingAnyItem)
                {
                    _flaskItemsMover.OnAnyItemMovingEnd += OnAnyItemMovingEnd;
                }
                else
                {
                    OnLevelCompleted();
                }

                void OnAnyItemMovingEnd()
                {
                    _flaskItemsMover.OnAnyItemMovingEnd -= OnAnyItemMovingEnd;

                    OnLevelCompleted();
                }

                void OnLevelCompleted()
                {
                    SetLevelStateByIndex(_currentLevelIndex, LevelState.Completed);

                    LevelCompleted?.Invoke(AllLevels[_currentLevelIndex]);

                    // Перед тем как записать новый текущий ключ, удалим предыдущую сохранённую запись уровня (чтобы в ISaves оставался только новый).
                    SaveLevelKey(_currentLevelIndex + 1);
                }
            }
        }

        private void ClearCurrentLevelView()
        {
            if (SpawnedFlasks.Count == 0)
                return;

            for (int i = 0; i < SpawnedFlasks.Count; i++)
            {
                Flask flask = SpawnedFlasks[i];

                if (flask != null)
                {
                    flask.OnFilled -= OnFilledFlask;
                    Destroy(flask.gameObject);
                }
            }

            SpawnedFlasks.Clear();

            _currentSpawnFlasksRow = 0;
        }

        private Item FindItemPrefab(string fruitName)
        {
            if (string.IsNullOrEmpty(fruitName))
                return null;

            return _itemPrefabs.Find(x => string.Equals(x.ItemName, fruitName, StringComparison.Ordinal));
        }

        private void SaveCurrentLevelKey()
        {
            if (_saves == null)
                return;

            if (_currentLevelIndex < 0 || _currentLevelIndex >= _loadedLevelKeys.Count)
                return;

            try
            {
                // _currentLevelIndex — индекс в AllLevels/_loadedLevelKeys
                string key = _loadedLevelKeys[_currentLevelIndex];
                if (!string.IsNullOrEmpty(key))
                {
                    _saves.SetString(SaveKey_CurrentLevel, key);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[LevelCreator] Не удалось сохранить текущий ключ уровня: {ex.Message}");
            }
        }

        private void SaveLevelKey(int levelIndex)
        {
            if (_saves == null)
                return;

            // Здесь levelIndex трактуется как индекс в AllLevels/_loadedLevelKeys
            if (levelIndex < 0 || levelIndex >= _loadedLevelKeys.Count)
                return;

            try
            {
                string newKey = _loadedLevelKeys[levelIndex];
                if (string.IsNullOrEmpty(newKey))
                    return;

                // Удалим предыдущую сохранённую запись уровня (если была), чтобы в ISaves оставался только последний открытый.
                try
                {
                    if (_saves.HasKey(SaveKey_CurrentLevel))
                    {
                        string prevKey = _saves.GetString(SaveKey_CurrentLevel);
                        if (!string.IsNullOrEmpty(prevKey) && prevKey != newKey)
                        {
                            try
                            {
                                _saves.SetObject<LevelData>(prevKey, null, prettyPrint: false);
                            }
                            catch (Exception ex)
                            {
                                Debug.LogWarning($"[LevelCreator] Не удалось удалить предыдущую запись уровня '{prevKey}': {ex.Message}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[LevelCreator] Ошибка при попытке удалить предыдущий ключ уровня: {ex.Message}");
                }

                // Установим новый текущий ключ и сохраним
                _saves.SetString(SaveKey_CurrentLevel, newKey);
                _saves.Save();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[LevelCreator] Не удалось сохранить текущий ключ уровня: {ex.Message}");
            }
        }

        [Inject]
        private void Initialize(ISaves saves, FlaskItemsMover flaskItemsMover, IAudioService audioService, IGameEvents gameEvents)
        {
            _saves = saves;
            _flaskItemsMover = flaskItemsMover;
            _audioService = audioService;
            _gameEvents = gameEvents;
        }
    }

    [Serializable]
    public class LevelData
    {
        public int LevelIndex;
        public int FlaskCapacity;
        public List<List<string>> Flasks = new List<List<string>>();
        public LevelState LevelState = LevelState.Locked;
    }

    public enum LevelState
    {
        Opened, Locked, Completed
    }
}
