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

        [Header("References")]
        [SerializeField] private Flask _flaskPrefab;
        [SerializeField] private Transform _startCreateFlasksPoint;
        [Space]
        [SerializeField] private List<Item> _itemPrefabs;

        [Header("Settings")]
        [SerializeField, Min(0)] private float _spawnOffsetBetweenFlasks;
        [SerializeField] private LevelGenerationSettings _generationSettings; // ScriptableObject с ключами уровней

        private ISaves _saves;
        private readonly List<Flask> _spawnedFlasks = new List<Flask>();

        private async void Awake()
        {
            await LoadAllLevelsFromAddressables();
            if (AllLevels.Count > 0)
            {
                CreateLevelView(AllLevels[0]);
            }
            else
            {
                Debug.LogWarning("[LevelCreator] Нет загруженных уровней.");
            }
        }

        private async Task LoadAllLevelsFromAddressables()
        {
            AllLevels.Clear();

            if (_generationSettings == null)
            {
                Debug.LogWarning("[LevelCreator] Не назначен LevelGenerationSettings для загрузки уровней.");
                return;
            }

            if (_generationSettings.GeneratedLevelKeys == null || _generationSettings.GeneratedLevelKeys.Count == 0)
            {
                Debug.LogWarning("[LevelCreator] Список ключей уровней пуст.");
                return;
            }

            foreach (string key in _generationSettings.GeneratedLevelKeys)
            {
                if (string.IsNullOrWhiteSpace(key))
                    continue;

                AsyncOperationHandle<TextAsset> handle = Addressables.LoadAssetAsync<TextAsset>(key);
                await handle.Task;

                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    try
                    {
                        var json = handle.Result.text;
                        var levelData = JsonConvert.DeserializeObject<LevelData>(json);
                        if (levelData != null)
                        {
                            AllLevels.Add(levelData);
                        }
                        else
                        {
                            Debug.LogError($"[LevelCreator] Не удалось десериализовать уровень из ключа '{key}'.");
                        }
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
            }

            Debug.Log($"[LevelCreator] Загружено уровней: {AllLevels.Count}");
        }

        /// <summary>
        /// Создаёт визуальное представление уровня по данным <see cref="LevelData"/>.
        /// Каждая внутренняя коллекция Flasks[i] содержит список фруктов (строки) снизу -> вверх.
        /// </summary>
        /// <param name="levelData">Данные уровня.</param>
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

            // Создание колб по горизонтали от стартовой точки.
            for (int i = 0; i < levelData.Flasks.Count; i++)
            {
                Vector3 spawnPos = _startCreateFlasksPoint.position + new Vector3(i * _spawnOffsetBetweenFlasks, 0f, 0f);
                Flask flaskInstance = Instantiate(_flaskPrefab, spawnPos, Quaternion.identity, _startCreateFlasksPoint.parent);
                _spawnedFlasks.Add(flaskInstance);

                List<string> fruitsInFlask = levelData.Flasks[i];
                if (fruitsInFlask == null || fruitsInFlask.Count == 0)
                    continue; // Пустая колба

                // Добавляем фрукты снизу -> вверх. TryAddItem кладёт в первый свободный слот (нижний).
                for (int j = 0; j < fruitsInFlask.Count; j++)
                {
                    string fruitName = fruitsInFlask[j];
                    Item prefab = FindItemPrefab(fruitName);
                    if (prefab == null)
                    {
                        Debug.LogWarning($"[LevelCreator] Не найден префаб фрукта '{fruitName}'. Пропуск.");
                        continue;
                    }

                    // Получаем трансформ свободного слота, чтобы правильно позиционировать инстанс.
                    Transform slotTransform = flaskInstance.GetFirstEmptySlotTransform();
                    if (slotTransform == null)
                    {
                        Debug.LogWarning($"[LevelCreator] Нет свободного слота в колбе {i + 1} при добавлении '{fruitName}'.");
                        break;
                    }

                    Item itemInstance = Instantiate(prefab, slotTransform.position, Quaternion.identity);
                    // Назначаем родителя слота – для локального позиционирования и удобства.
                    itemInstance.transform.SetParent(slotTransform, worldPositionStays: false);
                    itemInstance.transform.localPosition = Vector3.zero;

                    bool added = flaskInstance.TryAddItem(itemInstance);
                    if (!added)
                    {
                        // Если по какой-то причине не добавилось – удаляем инстанс чтобы не висел.
                        Debug.LogWarning($"[LevelCreator] TryAddItem вернул false для '{fruitName}' в колбе {i + 1}.");
                        Destroy(itemInstance.gameObject);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Удаляет ранее созданные объекты колб и их предметов.
        /// </summary>
        private void ClearCurrentLevelView()
        {
            if (_spawnedFlasks.Count == 0)
                return;

            for (int i = 0; i < _spawnedFlasks.Count; i++)
            {
                Flask flask = _spawnedFlasks[i];
                if (flask == null) continue;
                Destroy(flask.gameObject);
            }
            _spawnedFlasks.Clear();
        }

        /// <summary>
        /// Поиск префаба Item по имени фрукта (ItemName).
        /// </summary>
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

    /// <summary>
    /// Данные уровня для сериализации в json.
    /// Порядок фруктов в каждой колбе: снизу -> вверх (index 0 = нижний слот).
    /// </summary>
    [Serializable]
    public class LevelData
    {
        public int LevelIndex;
        public int FlaskCapacity;
        public List<List<string>> Flasks = new List<List<string>>();
    }
}
