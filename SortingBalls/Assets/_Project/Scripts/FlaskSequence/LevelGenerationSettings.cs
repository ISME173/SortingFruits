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

        [Tooltip("Дополнительные (помимо обязательной) пустые колбы. 0 означает что только одна пустая.")]
        [Min(0)] public int MinExtraEmptyFlasks = 0;
        [Min(0)] public int MaxExtraEmptyFlasks = 1;

        [Min(1), Tooltip("Вместимость одной колбы (слотов). Обычно = 4.")] 
        public int FlaskCapacity = 4;

        [Header("Рост сложности")]
        [Tooltip("Через сколько уровней увеличивать количество типов фруктов на 1.")]
        [Min(1)] public int LevelsPerFruitIncrease = 3;

        [Tooltip("Через сколько уровней потенциально увеличивается количество дополнительных пустых колб.")]
        [Min(1)] public int LevelsPerExtraEmptyFlaskIncrease = 7;

        [Header("Требования к сложности решения")]
        [Tooltip("Минимальное число ходов решения (по оценке BFS). Если решение короче — уровень регенерируется.")]
        [Min(0)] public int MinSolutionMoves = 8;

        [Tooltip("Ограничение на количество посещённых состояний при проверке решаемости (BFS).")]
        [Min(1000)] public int MaxSolverStates = 250000;

        [Tooltip("Использовать проверку решаемости через BFS (иначе уровни просто принимаются).")]
        public bool UseSolverValidation = true;

        [Header("Попытки генерации")]
        [Tooltip("Максимум попыток создать валидный уровень.")]
        [Min(1)] public int MaxGenerationAttemptsPerLevel = 40;

        [Header("Фрукты (идентификаторы). Убедитесь что каждый уникален.")]
        public List<string> AvailableFruitNames = new List<string>()
        {
            "Apple","Orange","Banana","Pear","Grape","Cherry","Kiwi","Lemon","Plum","Mango"
        };

        [Header("Проверки качества")]
        public bool AvoidAlmostSolved = true;
        public bool BreakSolvedFlasks = true;

        [Header("Addressables")]
        [Tooltip("Список ключей Addressables для сгенерированных уровней (например: Level_1, Level_2). Заполняется генератором.")]
        public List<string> GeneratedLevelKeys = new List<string>();
    }
}