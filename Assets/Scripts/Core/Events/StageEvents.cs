using System;
using System.Collections.Generic;

// 스테이지 관련 이벤트
public static class StageEvents
{
    public static event Action<StageNode> OnSubwayStarted;                // 지하철 출발 (향하는 역 노드)
    public static event Action<StageNode> OnStationArrived;               // 역 도착 (도착한 역 노드)
    public static event Action OnAllEnemiesDefeated;                      // 지하철 내 모든 적 처치
    public static event Action OnInteractionUnlocked;                     // 역 상호작용 해제 (적 전멸 시)
    public static event Action<List<StageNode>> OnRouteSelectionRequired; // 다음 역 선택 필요 (분기 2개 이상)

    public static void SubwayStarted(StageNode node)                       => OnSubwayStarted?.Invoke(node);
    public static void StationArrived(StageNode node)                      => OnStationArrived?.Invoke(node);
    public static void AllEnemiesDefeated()                                => OnAllEnemiesDefeated?.Invoke();
    public static void InteractionUnlocked()                               => OnInteractionUnlocked?.Invoke();
    public static void RouteSelectionRequired(List<StageNode> nextNodes)   => OnRouteSelectionRequired?.Invoke(nextNodes);
}
