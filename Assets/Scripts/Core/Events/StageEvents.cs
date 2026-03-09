using System;
using System.Collections.Generic;

public enum MapOpenReason { ViewOnly, RouteSelection }

// 스테이지 관련 이벤트
public static class StageEvents
{
    public static event Action<StageNode> OnSubwayStarted;                              // 지하철 출발
    public static event Action<StageNode> OnStationArrived;                             // 역 도착
    public static event Action OnAllEnemiesDefeated;                                    // 지하철 내 모든 적 처치
    public static event Action<StageNode> OnStationSkipped;                            // 적 생존으로 역 스킵 발생
    public static event Action<StageMap> OnMapGenerated;                                // 맵 생성 완료
    public static event Action<MapOpenReason, List<StageNode>> OnMapOpenRequested;      // 맵 UI 열기 요청

    public static void SubwayStarted(StageNode node)                                    => OnSubwayStarted?.Invoke(node);
    public static void StationArrived(StageNode node)                                   => OnStationArrived?.Invoke(node);
    public static void AllEnemiesDefeated()                                             => OnAllEnemiesDefeated?.Invoke();
    public static void StationSkipped(StageNode node)                                   => OnStationSkipped?.Invoke(node);
    public static void MapGenerated(StageMap map)                                       => OnMapGenerated?.Invoke(map);
    public static void MapOpenRequested(MapOpenReason reason, List<StageNode> nodes)    => OnMapOpenRequested?.Invoke(reason, nodes);
}
