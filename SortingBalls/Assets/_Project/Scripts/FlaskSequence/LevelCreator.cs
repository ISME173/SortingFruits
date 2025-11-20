using System;
using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.FlaskSequence
{
    public class LevelCreator : MonoBehaviour
    {
        private readonly List<LevelData> AllLevels = new List<LevelData>();

        [Header("References")]
        [SerializeField] private Flask _flaskPrefab;
        [SerializeField] private Transform _startCreateFlasksPoint;

        [Header("Settings")]
        [SerializeField, Min(0)] private float _spawnOffsetBetweenFlasks;

        private void Awake()
        {
            
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
