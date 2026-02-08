using System;

// 스테이지 관련 이벤트
public static class StageEvents
{
    public static event Action<int> OnStageStart;        // 스테이지 번호
    public static event Action<int> OnStageClear;        // 스테이지 번호
    public static event Action OnAllEnemiesDefeated;

    public static void StageStart(int stageNumber) => OnStageStart?.Invoke(stageNumber);
    public static void StageClear(int stageNumber) => OnStageClear?.Invoke(stageNumber);
    public static void AllEnemiesDefeated() => OnAllEnemiesDefeated?.Invoke();
}
