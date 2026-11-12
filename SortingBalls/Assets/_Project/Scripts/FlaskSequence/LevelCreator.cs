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
        private int _frontierLevelIndex = -1;
        private int _levelLoadVersion;
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

            string currentKey = GetOriginalKey(_frontierLevelIndex);
            LevelData currentLevel = FindLevelData(_frontierLevelIndex);
            if (string.IsNullOrEmpty(currentKey) || currentLevel == null)
                return;

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

                // Сохраняем только крайний открытый уровень. Повтор старых уровней его не подменяет.
                try
                {
                    _saves.SetObject(currentKey, currentLevel, prettyPrint: true);
                    _saves.SetString(SaveKey_CurrentLevel, currentKey);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[LevelCreator] Не удалось сохранить текущий уровень '{currentKey}' в ISaves: {ex.Message}");
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
                _frontierLevelIndex = _currentLevelIndex;

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
            // Теперь мы НЕ сохраняем все загруженные уровни автоматически. Сохраняем только крайний открытый уровень через SaveLevels.
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
            if (_frontierLevelIndex < 0)
            {
                Debug.LogWarning("[LevelCreator] Уровни ещё загружаются.");
                return;
            }

            int nextIndex = _currentLevelIndex + 1;
            if (nextIndex < 0 || nextIndex >= LevelsCount)
            {
                Debug.LogWarning("[LevelCreator] Нет следующего уровня.");
                return;
            }

            LevelData loaded = FindLevelData(nextIndex);
            if (loaded != null)
            {
                ShowLevel(nextIndex, loaded, advanceFrontier: true);
                return;
            }

            if (!_allLevelsLoaded && (_frontierLevelIndex < 0 || nextIndex >= _frontierLevelIndex))
            {
                Debug.LogWarning("[LevelCreator] Следующий уровень ещё не загружен.");
                return;
            }

            _ = LoadAndShowAsync(nextIndex, advanceFrontier: true);
        }

        [ContextMenu("LevelsControll/ReloadCurrentLevel")]
        public void ReloadCurrentLevel()
        {
            _levelLoadVersion++;

            LevelData level = FindLevelData(_currentLevelIndex);
            if (level == null)
            {
                Debug.LogWarning("[LevelCreator] Текущий индекс уровня некорректен.");
                return;
            }

            CreateLevelView(level);
        }

        public void LoadLevelByIndex(int index)
        {
            if (!CanPlayLevel(index))
                return;

            LevelData loaded = FindLevelData(index);
            if (loaded != null)
            {
                ShowLevel(index, loaded, advanceFrontier: false);
                return;
            }

            _ = LoadAndShowAsync(index, advanceFrontier: false);
        }

        private async Task LoadAndShowAsync(int index, bool advanceFrontier)
        {
            int version = ++_levelLoadVersion;

            try
            {
                LevelData level = await EnsureLevelDataAsync(index);
                if (level == null || version != _levelLoadVersion)
                {
                    if (level == null)
                        Debug.LogWarning($"[LevelCreator] Не удалось загрузить уровень {index + 1}.");
                    return;
                }

                ShowLevel(index, level, advanceFrontier);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LevelCreator] Ошибка загрузки уровня {index + 1}: {ex.Message}");
            }
        }

        private void ShowLevel(int index, LevelData level, bool advanceFrontier)
        {
            if (level == null)
                return;

            // Отменяет загрузку, которая ещё не успела показать уровень.
            _levelLoadVersion++;

            if (advanceFrontier && index > _frontierLevelIndex)
                _frontierLevelIndex = index;

            _currentLevelIndex = index;

            bool isReplay = level.LevelState == LevelState.Completed
                || (_frontierLevelIndex >= 0 && index < _frontierLevelIndex);
            if (isReplay)
            {
                CreateLevelView(level);
                return;
            }

            SetLevelStateByIndex(index, LevelState.Opened);
            CreateLevelView(level);
            SaveFrontierKey();
        }

        private bool CanPlayLevel(int index)
        {
            if (_frontierLevelIndex < 0)
            {
                Debug.LogWarning("[LevelCreator] Уровни ещё загружаются.");
                return false;
            }

            if (_generationSettings == null || index < 0 || index >= LevelsCount)
            {
                Debug.LogWarning($"[LevelCreator] Индекс {index} вне диапазона уровней.");
                return false;
            }

            if (index > _frontierLevelIndex)
            {
                Debug.LogWarning($"[LevelCreator] Уровень {index + 1} ещё закрыт.");
                return false;
            }

            return true;
        }

        public void NotifyLevelStates()
        {
            if (_frontierLevelIndex < 0 || LevelsCount <= 0)
                return;

            for (int i = 0; i < LevelsCount; i++)
            {
                LevelState state;
                if (i < _frontierLevelIndex)
                    state = LevelState.Completed;
                else if (i == _frontierLevelIndex)
                    state = LevelState.Opened;
                else
                    state = LevelState.Locked;

                OnLevelStateChanged?.Invoke(i, state);
            }
        }

        private async Task<LevelData> EnsureLevelDataAsync(int originalIndex)
        {
            LevelData existing = FindLevelData(originalIndex);
            if (existing != null)
                return existing;

            string key = GetOriginalKey(originalIndex);
            if (string.IsNullOrEmpty(key))
                return null;

            LevelData level = await LoadLevelByKey(key);
            if (level == null)
                return null;

            existing = FindLevelData(originalIndex);
            if (existing != null)
                return existing;

            if (originalIndex < _frontierLevelIndex)
                level.LevelState = LevelState.Completed;
            else if (originalIndex == _frontierLevelIndex)
                level.LevelState = LevelState.Opened;
            else
                level.LevelState = LevelState.Locked;

            AllLevels.Add(level);
            if (!_loadedLevelKeys.Contains(key))
                _loadedLevelKeys.Add(key);

            return level;
        }

        private LevelData FindLevelData(int originalIndex)
        {
            return AllLevels.FirstOrDefault(level => level != null && level.LevelIndex - 1 == originalIndex);
        }

        private string GetOriginalKey(int originalIndex)
        {
            if (_generationSettings == null || _generationSettings.GeneratedLevelKeys == null)
                return null;

            if (originalIndex < 0 || originalIndex >= _generationSettings.GeneratedLevelKeys.Count)
                return null;

            return _generationSettings.GeneratedLevelKeys[originalIndex];
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
                    LevelData completedLevel = FindLevelData(_currentLevelIndex);
                    bool advancesProgress = _frontierLevelIndex < 0 || _currentLevelIndex >= _frontierLevelIndex;

                    if (advancesProgress)
                        SetLevelStateByIndex(_currentLevelIndex, LevelState.Completed);

                    if (completedLevel != null)
                        LevelCompleted?.Invoke(completedLevel);
                    else
                        Debug.LogWarning($"[LevelCreator] Не найдены данные завершённого уровня {_currentLevelIndex + 1}.");

                    if (advancesProgress)
                        AdvanceFrontier(_currentLevelIndex + 1);
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
            SaveFrontierKey();
        }

        private void SaveFrontierKey()
        {
            if (_saves == null)
                return;

            string key = GetOriginalKey(_frontierLevelIndex);
            if (string.IsNullOrEmpty(key))
                return;

            try
            {
                _saves.SetString(SaveKey_CurrentLevel, key);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[LevelCreator] Не удалось сохранить текущий ключ уровня: {ex.Message}");
            }
        }

        private void AdvanceFrontier(int levelIndex)
        {
            if (levelIndex < 0 || levelIndex >= LevelsCount || levelIndex <= _frontierLevelIndex)
                return;

            string previousKey = GetOriginalKey(_frontierLevelIndex);
            _frontierLevelIndex = levelIndex;

            if (_saves == null)
                return;

            try
            {
                if (!string.IsNullOrEmpty(previousKey))
                {
                    try
                    {
                        _saves.SetObject<LevelData>(previousKey, null, prettyPrint: false);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[LevelCreator] Не удалось удалить предыдущую запись уровня '{previousKey}': {ex.Message}");
                    }
                }

                SaveFrontierKey();
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
