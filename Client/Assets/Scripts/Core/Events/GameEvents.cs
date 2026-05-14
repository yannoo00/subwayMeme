using System;

// 게임 흐름 관련 이벤트
public static class GameEvents
{
    public static event Action OnGameStart;
    public static event Action OnGameOver;
    public static event Action OnGameClear;
    public static event Action OnGamePaused;
    public static event Action OnGameResumed;
    

    // current: 현재 HP, max: 최대 HP
    public static event Action<int, int> OnGeneratorDamaged;

    public static void GameStart() => OnGameStart?.Invoke();
    public static void GameOver() => OnGameOver?.Invoke();
    public static void GameClear() => OnGameClear?.Invoke();
    public static void GamePaused() => OnGamePaused?.Invoke();
    public static void GameResumed() => OnGameResumed?.Invoke();
    public static void GeneratorDamaged(int current, int max) => OnGeneratorDamaged?.Invoke(current, max);
    
}
