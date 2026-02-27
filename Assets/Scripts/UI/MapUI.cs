using System.Collections.Generic;
using UnityEngine;


// 노선 UI 전체를 관리하는 컴포넌트
// StageEvents.OnMapGenerated:      BuildMap으로 노선도 비주얼 구성
// StageEvents.OnMapOpenRequested:  맵 패널 열기 (RouteSelection: 역 선택 강제 / ViewOnly: 자유롭게 닫기 가능)
// StageEvents.OnStationArrived:    현재 노드 및 지나온 경로 상태 갱신

public class MapUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject _panel;
    [SerializeField] private RectTransform _mapArea;  // 노드/경로가 배치될 영역

    [Header("Prefabs")]
    [SerializeField] private NodeVisual _nodeVisualPrefab;
    [SerializeField] private PathVisual _pathVisualPrefab;

    [Header("Layout")]
    [SerializeField] private float _horizontalSpacing = 100f;
    [SerializeField] private float _verticalSpacing = 80f;


    private Dictionary<StageNode, NodeVisual> _nodeVisuals = new();
    private Dictionary<(StageNode, StageNode), PathVisual> _pathVisuals = new();

    private List<StageNode> _currentAvailableNodes = new();
    private MapOpenReason _currentReason;



    private void OnEnable()
    {
        StageEvents.OnMapGenerated          += BuildMap;
        StageEvents.OnMapOpenRequested      += OnMapOpenRequested;
        StageEvents.OnStationArrived        += OnStationArrived;
    }

    private void OnDisable()
    {
        StageEvents.OnMapGenerated          -= BuildMap;
        StageEvents.OnMapOpenRequested      -= OnMapOpenRequested;
        StageEvents.OnStationArrived        -= OnStationArrived;
    }




    // 맵 생성 완료 시 1회 호출하여 전체 노선도 비주얼 구성
    private void BuildMap(StageMap map)
    {
        // 이전 비주얼 제거
        foreach (Transform child in _mapArea)
            Destroy(child.gameObject);
        _nodeVisuals.Clear();
        _pathVisuals.Clear();

        // 경로를 먼저 생성해야 노드 아래에 그려짐
        foreach (var floorList in map.floors)
            foreach (var node in floorList)
                foreach (var nextNode in node.nextNodes)
                    CreatePathVisual(node, nextNode);

        // 연결된 노드만 생성
        foreach (var floorList in map.floors)
            foreach (var node in floorList)
                if (node.nextNodes.Count > 0 || node.previousNodes.Count > 0)
                    CreateNodeVisual(node);

        _panel.SetActive(false);
    }

    private void CreateNodeVisual(StageNode node)
    {
        NodeVisual visual = Instantiate(_nodeVisualPrefab, _mapArea);
        visual.GetComponent<RectTransform>().anchoredPosition = GetNodePosition(node);
        visual.Setup(node, OnNodeSelected);
        _nodeVisuals[node] = visual;
    }

    private void CreatePathVisual(StageNode from, StageNode to)
    {
        Vector2 fromPos = GetNodePosition(from);
        Vector2 toPos = GetNodePosition(to);

        PathVisual visual = Instantiate(_pathVisualPrefab, _mapArea);
        RectTransform rt = visual.GetComponent<RectTransform>();

        rt.anchoredPosition = (fromPos + toPos) / 2f;
        // x: 두 노드 사이 거리, y: 프리팹에 설정한 선 두께 유지
        rt.sizeDelta = new Vector2(Vector2.Distance(fromPos, toPos), rt.sizeDelta.y);
        rt.localRotation = Quaternion.Euler(0, 0,
            Mathf.Atan2(toPos.y - fromPos.y, toPos.x - fromPos.x) * Mathf.Rad2Deg);

        _pathVisuals[(from, to)] = visual;
    }

    // floor/column 값을 mapArea 내 2D 좌표로 변환
    private Vector2 GetNodePosition(StageNode node)
    {
        return new Vector2(node.column * _horizontalSpacing, node.floor * _verticalSpacing);
    }



    private void OnMapOpenRequested(MapOpenReason reason, List<StageNode> nextNodes)
    {
        // 패널이 열려있는 경우
        if (_panel.activeSelf)
        {
            // ViewOnly로 열려있을 때 M키 재입력 → 닫기 (토글)
            if (reason == MapOpenReason.ViewOnly && _currentReason == MapOpenReason.ViewOnly)
                CloseMap();

            // 그 외(RouteSelection 중 M키 등)는 무시
            return;
        }

        _currentReason = reason;
        _panel.SetActive(true);

        if (reason == MapOpenReason.RouteSelection)
        {
            _currentAvailableNodes = nextNodes;
            foreach (var node in nextNodes)
                if (_nodeVisuals.TryGetValue(node, out NodeVisual visual))
                    visual.SetState(NodeState.Available);
        }
    }

    private void CloseMap()
    {
        _panel.SetActive(false);
    }

    // 역 도착 시 현재 노드와 지나온 경로 갱신
    private void OnStationArrived(StageNode currentNode)
    {
        if (_nodeVisuals.TryGetValue(currentNode, out NodeVisual currentVisual))
            currentVisual.SetState(NodeState.Current);

        // 이전 노드 to 현재 노드 경로를 Visited로 표시
        foreach (var prevNode in currentNode.previousNodes)
            if (_pathVisuals.TryGetValue((prevNode, currentNode), out PathVisual pathVisual))
                pathVisual.SetVisited(true);
    }

    // 노드 버튼 클릭 시 처리
    private void OnNodeSelected(StageNode node)
    {
        _panel.SetActive(false);

        // 선택되지 않은 Available 노드들을 Normal로 복원
        // (선택된 노드는 OnStationArrived에서 Current로 전환됨)
        foreach (var availNode in _currentAvailableNodes)
            if (availNode != node && _nodeVisuals.TryGetValue(availNode, out NodeVisual visual))
                visual.SetState(NodeState.Normal);
        _currentAvailableNodes.Clear();

        StageManager.Instance.MoveToNode(node);
    }
}
