using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelGenerationSettings", menuName = "Levels/LevelGenerationSettings")]
public class LevelGenerationSettings : ScriptableObject
{
    [Header("Параметры сложности")]
    [Min(1)] public int DifficultyCapLevel = 100; // уровень, после которого рост сложности прекращается
    [Min(0)] public int PostCapJitterRange = 5;   // ± дрожание «эффективного уровня сложности» после капа

    [Header("Базовые параметры")]
    [Min(2)] public int MinFruitTypes = 3;
    [Min(2)] public int MaxFruitTypes = 10;

    [Tooltip("Дополнительные (сверх обязательной) пустые колбы.")]
    [Min(0)] public int MinExtraEmptyFlasks = 0;
    [Min(0)] public int MaxExtraEmptyFlasks = 1;

    [Min(1)] public int FlaskCapacity = 4;

    [Header("Рост параметров")]
    [Min(1)] public int LevelsPerFruitIncrease = 3;
    [Min(1)] public int LevelsPerExtraEmptyFlaskIncrease = 7;

    [Header("Валидность")]
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

    [Header("Фильтрация уровней")]
    public bool AvoidAlmostSolved = true;
    public bool BreakSolvedFlasks = true;

    [Header("Addressables")]
    public List<string> GeneratedLevelKeys = new List<string>();

    [Header("Ограничения")]
    [Min(2)] public int MaxFlasksPerLevel = 12;

    [Header("Служебные поля генерации")]
    [Tooltip("UTC Ticks последней генерации уровней (диагностика производительности).")]
    public long LastGenerationTimestamp;
    [Tooltip("Если требуется принудительно перезагрузить генератор уровней из JSON, поставить true. Сбрасывается после генерации.")]
    public bool ForceReloadOnNextPlay;
}