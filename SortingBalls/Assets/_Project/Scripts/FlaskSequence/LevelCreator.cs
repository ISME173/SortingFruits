using _Project.Scripts.Saves;
using Reflex.Attributes;
using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Threading.Tasks;

namespace _Project.Scripts.FlaskSequence
{
    public class LevelCreator : MonoBehaviour
    {
        private readonly List<LevelData> AllLevels = new List<LevelData>();
        private readonly List<Flask> SpawnedFlasks = new List<Flask>();

        [Header("References")]
        [SerializeField] private Flask _flaskPrefab;
        [SerializeField] private Transform _startCreateFlasksPoint;
        [Space]
        [SerializeField] private List<Item> _itemPrefabs;

        [Header("Settings")]
        [SerializeField, Min(0)] private float _spawnOffsetBetweenFlasks;
        [SerializeField, Min(0)] private float _spawnRowOffsetY = 2f; // Смещение по Y между рядами (новое)
        [SerializeField] private LevelGenerationSettings _generationSettings;

        private ISaves _saves;
        private int _currentLevelIndex = -1;
        private bool _allLevelsLoaded = false;

        public event Action<LevelData> LevelCreated;

        public int CurrentLevelIndex => _currentLevelIndex;
        public int LevelsCount => AllLevels.Count;


        private async void Awake()
        {
            await LoadFirstLevelAndCreateView();
            _ = LoadRemainingLevels();
        }

        #region Загрузка уровней

        private async Task LoadFirstLevelAndCreateView()
        {
            if (_generationSettings == null || _generationSettings.GeneratedLevelKeys == null || _generationSettings.GeneratedLevelKeys.Count == 0)
            {
                Debug.LogWarning("[LevelCreator] Нет настроек или списка ключей уровней.");
                return;
            }

            string firstKey = _generationSettings.GeneratedLevelKeys[0];
            LevelData level = await LoadLevelByKey(firstKey);
            if (level != null)
            {
                AllLevels.Add(level);
                _currentLevelIndex = 0;
                CreateLevelView(level);
            }
            else
            {
                Debug.LogError($"[LevelCreator] Не удалось загрузить первый уровень по ключу '{firstKey}'.");
            }
        }

        private async Task LoadRemainingLevels()
        {
            if (_generationSettings == null || _generationSettings.GeneratedLevelKeys == null)
                return;

            for (int i = 1; i < _generationSettings.GeneratedLevelKeys.Count; i++)
            {
                string key = _generationSettings.GeneratedLevelKeys[i];
                LevelData level = await LoadLevelByKey(key);
                if (level != null)
                    AllLevels.Add(level);
            }

            _allLevelsLoaded = true;
            Debug.Log($"[LevelCreator] Все уровни загружены. Всего: {AllLevels.Count}");
        }

        private async Task<LevelData> LoadLevelByKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return null;

            if (_saves != null && _saves.HasKey(key))
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

            if (result != null && _saves != null)
            {
                try
                {
                    _saves.SetObject(key, result, prettyPrint: true);
                    _saves.Save();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[LevelCreator] Не удалось сохранить уровень '{key}' в ISaves: {ex.Message}");
                }
            }

            return result;
        }

        #endregion

        #region Управление уровнями

        public void LoadNextLevel()
        {
            int nextIndex = _currentLevelIndex + 1;
            if (nextIndex >= AllLevels.Count)
            {
                Debug.Log(!_allLevelsLoaded
                    ? "[LevelCreator] Следующий уровень ещё не загружен."
                    : "[LevelCreator] Нет следующего уровня.");
                return;
            }

            _currentLevelIndex = nextIndex;
            CreateLevelView(AllLevels[_currentLevelIndex]);
        }

        public void ReloadCurrentLevel()
        {
            if (_currentLevelIndex < 0 || _currentLevelIndex >= AllLevels.Count)
            {
                Debug.LogWarning("[LevelCreator] Текущий индекс уровня некорректен.");
                return;
            }
            CreateLevelView(AllLevels[_currentLevelIndex]);
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
        }

        #endregion

        /// <summary>
        /// Создаёт визуальное представление уровня по данным <see cref="LevelData"/>.
        /// Максимум 3 колбы в ряд. Если больше, начинается новый ряд выше на _spawnRowOffsetY.
        /// Каждый ряд центрируется относительно _startCreateFlasksPoint.
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
            const int maxPerRow = 3;

            int created = 0;
            int row = 0;

            while (created < total)
            {
                int remaining = total - created;
                int inRow = remaining < maxPerRow ? remaining : maxPerRow;

                // Центрируем по горизонтали
                float startX = basePos.x - 0.5f * spacingX * (inRow - 1);
                float y = basePos.y + row * spacingY;

                for (int i = 0; i < inRow; i++)
                {
                    int flaskIndex = created + i;
                    float x = startX + i * spacingX;
                    Vector3 spawnPos = new Vector3(x, y, basePos.z);

                    Flask flaskInstance = Instantiate(_flaskPrefab, spawnPos, Quaternion.identity, _startCreateFlasksPoint.parent);
                    SpawnedFlasks.Add(flaskInstance);

                    List<string> fruitsInFlask = levelData.Flasks[flaskIndex];
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
                            Debug.LogWarning($"[LevelCreator] Нет свободного слота в колбе {flaskIndex + 1} при добавлении '{fruitName}'.");
                            break;
                        }

                        Item itemInstance = Instantiate(prefab, slotTransform.position, Quaternion.identity);
                        itemInstance.transform.SetParent(slotTransform, worldPositionStays: false);
                        itemInstance.transform.localPosition = Vector3.zero;

                        if (!flaskInstance.TryAddItem(itemInstance))
                        {
                            Debug.LogWarning($"[LevelCreator] TryAddItem вернул false для '{fruitName}' в колбе {flaskIndex + 1}.");
                            Destroy(itemInstance.gameObject);
                            break;
                        }
                    }
                }

                created += inRow;
                row++;
            }

            LevelCreated?.Invoke(levelData);
        }

        private void ClearCurrentLevelView()
        {
            if (SpawnedFlasks.Count == 0) return;

            for (int i = 0; i < SpawnedFlasks.Count; i++)
            {
                Flask flask = SpawnedFlasks[i];
                if (flask != null)
                    Destroy(flask.gameObject);
            }
            SpawnedFlasks.Clear();
        }

        private Item FindItemPrefab(string fruitName)
        {
            if (string.IsNullOrEmpty(fruitName))
                return null;

            return _itemPrefabs.Find(x => string.Equals(x.ItemName, fruitName, StringComparison.Ordinal));
        }

        [Inject]
        private void Initialize(ISaves saves)
        {
            _saves = saves;
        }
    }

    [Serializable]
    public class LevelData
    {
        public int LevelIndex;
        public int FlaskCapacity;
        public List<List<string>> Flasks = new List<List<string>>();
    }
}
