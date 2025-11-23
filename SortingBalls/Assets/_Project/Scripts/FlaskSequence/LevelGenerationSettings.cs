using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.FlaskSequence
{
    [CreateAssetMenu(fileName = "LevelGenerationSettings", menuName = "Levels/LevelGenerationSettings")]
    public class LevelGenerationSettings : ScriptableObject
    {
        [Header("Базовые параметры")]
        [Min(2)] public int MinFruitTypes = 3;
        [Min(2)] public int MaxFruitTypes = 10;

        [Tooltip("Дополнительные (помимо обязательной) пустые колбы.")]
        [Min(0)] public int MinExtraEmptyFlasks = 0;
        [Min(0)] public int MaxExtraEmptyFlasks = 1;

        [Min(1)] public int FlaskCapacity = 4;

        [Header("Рост сложности")]
        [Min(1)] public int LevelsPerFruitIncrease = 3;
        [Min(1)] public int LevelsPerExtraEmptyFlaskIncrease = 7;

        [Header("Решаемость")]
        [Min(0)] public int MinSolutionMoves = 6;
        [Min(1000)] public int MaxSolverStates = 250000;
        public bool UseSolverValidation = true;

        [Header("Попытки генерации")]
        [Min(1)] public int MaxGenerationAttemptsPerLevel = 100;

        [Header("Фрукты")]
        public List<string> AvailableFruitNames = new List<string>()
        {
            "Apple","Orange","Banana","Pear","Grape","Cherry","Kiwi","Lemon","Plum","Mango"
        };

        [Header("Проверки качества")]
        public bool AvoidAlmostSolved = true;
        public bool BreakSolvedFlasks = true;

        [Header("Addressables")]
        public List<string> GeneratedLevelKeys = new List<string>();

        [Header("Ограничения")]
        [Min(2)] public int MaxFlasksPerLevel = 12;

        [Header("Обновление после генерации")]
        [Tooltip("UTC Ticks последней генерации уровней (устанавливается генератором).")]
        public long LastGenerationTimestamp;
        [Tooltip("Флаг принудительной перезагрузки уровней из JSON, игнорируя сохранения. Сбрасывается после первого запуска.")]
        public bool ForceReloadOnNextPlay;
    }
}