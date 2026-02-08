using System;

// 게임 흐름 관련 이벤트
public static class GameEvents
{
    public static event Action OnGameStart;
    public static event Action OnGameOver;
    public static event Action OnGamePaused;
    public static event Action OnGameResumed;

    public static void GameStart() => OnGameStart?.Invoke();
    public static void GameOver() => OnGameOver?.Invoke();
    public static void GamePaused() => OnGamePaused?.Invoke();
    public static void GameResumed() => OnGameResumed?.Invoke();
}
