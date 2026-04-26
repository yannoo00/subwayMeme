using System.Collections.Generic;
using GameProto;
using UnityEngine;
using UnityEngine.UIElements;


// UI Toolkit 기반 노선도 UI
// 초기화 시 전체 격자(50노드 + 117경로)를 1회 생성
// 맵 생성 시에는 active 클래스 토글로 사용/미사용 전환
// 경로 위치는 GeometryChangedEvent마다 재계산 (스크롤바 등 레이아웃 변경 대응)
public class MapUIDocument : MonoBehaviour
{
    // === Inspector 변수 ===

    [Header("Grid Size")]
    [SerializeField] private int _columns = 5;
    [SerializeField] private int _floors = 10;

    [Header("Style")]
    [SerializeField] private StyleSheet _styleSheet;


    // === Private 변수 ===

    private UIDocument _document;
    private VisualElement _panel;
    private VisualElement _pathContainer;
    private VisualElement _nodeContainer;

    // 전체 격자 참조 (초기화 시 1회 생성, 이후 재사용)
    private Button[,] _allNodes;
    private Dictionary<(int, int, int, int), VisualElement> _allPaths = new();

    private List<StageNode> _currentAvailableNodes = new();
    private MapOpenReason _currentReason;
    private bool _isOpen;


    // === Unity 생명주기 ===

    private void Awake()
    {
        _document = GetComponent<UIDocument>();
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        BindUI();
        BuildGrid();

        _nodeContainer.RegisterCallback<GeometryChangedEvent>(OnNodeContainerGeometryChanged);

        StageEvents.OnMapGenerated     += OnMapGenerated;
        StageEvents.OnMapOpenRequested += OnMapOpenRequested;
        StageEvents.OnStationArrived   += OnStationArrived;
    }

    private void OnDisable()
    {
        _nodeContainer.UnregisterCallback<GeometryChangedEvent>(OnNodeContainerGeometryChanged);

        StageEvents.OnMapGenerated     -= OnMapGenerated;
        StageEvents.OnMapOpenRequested -= OnMapOpenRequested;
        StageEvents.OnStationArrived   -= OnStationArrived;
    }


    // === UI 바인딩 ===

    private void BindUI()
    {
        var root = _document.rootVisualElement;

        if (_styleSheet != null)
            root.styleSheets.Add(_styleSheet);

        _panel          = root.Q<VisualElement>("map-panel");
        _pathContainer  = root.Q<VisualElement>("path-container");
        _nodeContainer  = root.Q<VisualElement>("node-container");

        _panel.style.display = DisplayStyle.None;
        _isOpen = false;
    }


    // === 격자 초기 생성 (1회) ===

    // 전체 격자(노드 + 가능한 모든 경로)를 반복문으로 생성
    // 이후 맵 생성 시에는 요소를 만들거나 지우지 않고 active 클래스만 토글
    private void BuildGrid()
    {
        _allNodes = new Button[_floors, _columns];
        _allPaths.Clear();

        // 노드: floor 높은 순(위에서 아래)으로 floor-row 생성
        // Flexbox(justify-content: space-around)가 자동 배치
        for (int floor = _floors - 1; floor >= 0; floor--)
        {
            var row = new VisualElement();
            row.AddToClassList("floor-row");

            for (int col = 0; col < _columns; col++)
            {
                var node = new Button();
                node.name = $"node-{floor}-{col}";
                node.AddToClassList("node");
                row.Add(node);
                _allNodes[floor, col] = node;
            }

            _nodeContainer.Add(row);
        }

        // 경로: 인접 층 사이 가능한 모든 연결 (col +/- 1 범위)
        for (int floor = 0; floor < _floors - 1; floor++)
        {
            for (int fromCol = 0; fromCol < _columns; fromCol++)
            {
                int minCol = Mathf.Max(0, fromCol - 1);
                int maxCol = Mathf.Min(_columns - 1, fromCol + 1);

                for (int toCol = minCol; toCol <= maxCol; toCol++)
                {
                    var path = new VisualElement();
                    path.name = $"path-{floor}-{fromCol}-{floor + 1}-{toCol}";
                    path.AddToClassList("path");
                    _pathContainer.Add(path);
                    _allPaths[(floor, fromCol, floor + 1, toCol)] = path;
                }
            }
        }
    }


    // === 경로 위치 계산 ===

    // 노드의 실제 렌더링 위치를 기반으로 경로의 위치/회전/길이를 설정
    // GeometryChangedEvent마다 호출되어 레이아웃 변경에 대응
    private void PositionPaths()
    {
        foreach (var kvp in _allPaths)
        {
            var (fromFloor, fromCol, toFloor, toCol) = kvp.Key;
            VisualElement path = kvp.Value;

            Button fromNode = _allNodes[fromFloor, fromCol];
            Button toNode   = _allNodes[toFloor, toCol];

            // worldBound: 화면 기준 절대 좌표에서 path-container 기준 상대 좌표로 변환
            Vector2 containerPos = _pathContainer.worldBound.position;
            Vector2 fromCenter = fromNode.worldBound.center - containerPos;
            Vector2 toCenter   = toNode.worldBound.center - containerPos;

            Vector2 mid = (fromCenter + toCenter) / 2f;
            float dist  = Vector2.Distance(fromCenter, toCenter);
            float angle = Mathf.Atan2(toCenter.y - fromCenter.y, toCenter.x - fromCenter.x) * Mathf.Rad2Deg;

            path.style.width  = dist;
            path.style.left   = mid.x - dist / 2f;
            // top은 요소의 상단 가장자리 위치이므로 중심을 맞추려면 height/2(=1px) 빼기
            path.style.top    = mid.y - 1f;
            path.style.rotate = new StyleRotate(new Rotate(Angle.Degrees(angle)));
        }
    }

    // 레이아웃 width가 바뀔 때만 경로 위치 재계산
    private void OnNodeContainerGeometryChanged(GeometryChangedEvent evt)
    {
        if (evt.oldRect.width == evt.newRect.width) return;
        PositionPaths();
    }


    // === 맵 생성 이벤트 처리 ===

    private void OnMapGenerated(StageMap map)
    {
        // 모든 노드/경로의 active와 상태 클래스 제거
        ResetAll();

        // 맵에 포함된 노드만 활성화
        foreach (var floorList in map.floors)
        {
            foreach (var node in floorList)
            {
                Button btn = _allNodes[node.floor, node.column];
                btn.AddToClassList("active");
                btn.text = node.type.ToString();

                // 클릭 이벤트 등록
                btn.clickable = new Clickable(() => OnNodeSelected(node));
            }
        }

        // 맵에 포함된 경로만 활성화
        foreach (var floorList in map.floors)
        {
            foreach (var node in floorList)
            {
                foreach (var next in node.nextNodes)
                {
                    var key = (node.floor, node.column, next.floor, next.column);
                    if (_allPaths.TryGetValue(key, out VisualElement path))
                        path.AddToClassList("active");
                }
            }
        }

        _panel.style.display = DisplayStyle.None;
        _isOpen = false;
    }

    private void ResetAll()
    {
        for (int f = 0; f < _floors; f++)
        {
            for (int c = 0; c < _columns; c++)
            {
                Button btn = _allNodes[f, c];
                btn.RemoveFromClassList("active");
                btn.RemoveFromClassList("node--available");
                btn.RemoveFromClassList("node--current");
                btn.RemoveFromClassList("node--visited");
                btn.SetEnabled(false);
            }
        }

        foreach (var path in _allPaths.Values)
        {
            path.RemoveFromClassList("active");
            path.RemoveFromClassList("path--visited");
        }
    }


    // === 패널 열기/닫기 ===

    private void OnMapOpenRequested(MapOpenReason reason, List<StageNode> nextNodes)
    {
        if (_isOpen)
        {
            if (reason == MapOpenReason.ViewOnly && _currentReason == MapOpenReason.ViewOnly)
                CloseMap();
            return;
        }

        _currentReason = reason;
        _panel.style.display = DisplayStyle.Flex;
        _isOpen = true;
        UnityEngine.Cursor.lockState = CursorLockMode.None;
        UnityEngine.Cursor.visible   = true;

        if (reason == MapOpenReason.RouteSelection)
        {
            _currentAvailableNodes = new List<StageNode>(nextNodes);
            foreach (var node in nextNodes)
                SetNodeState(node, NodeState.Available);
        }
    }

    private void CloseMap()
    {
        _panel.style.display = DisplayStyle.None;
        _isOpen = false;
        UnityEngine.Cursor.lockState = UnityEngine.CursorLockMode.Locked;
        UnityEngine.Cursor.visible   = false;
    }


    // === 상태 갱신 ===

    private void OnStationArrived(StageNode currentNode)
    {
        SetNodeState(currentNode, NodeState.Current);

        foreach (var prev in currentNode.previousNodes)
        {
            var key = (prev.floor, prev.column, currentNode.floor, currentNode.column);
            if (_allPaths.TryGetValue(key, out VisualElement path))
                path.AddToClassList("path--visited");
        }
    }

    private void OnNodeSelected(StageNode node)
    {
        CloseMap();

        foreach (var availNode in _currentAvailableNodes)
            if (availNode != node)
                SetNodeState(availNode, NodeState.Normal);
        _currentAvailableNodes.Clear();

        // 씬 전환은 S_AllBoarded 수신 후 처리되므로 여기서는 서버에 선택만 전달
        // 여기서 직접 moveToNode 하지 않고 서버에서 전원 탑승 수신 후 이동한다. 
        int nodeIndex = StageManager.Instance.GetIndex(node);
        NetworkManager.Instance.SendGame(GamePacketId.CSelectRoute, new C_SelectRoute { NodeIndex = nodeIndex });
    }

    // 노드의 상태 클래스만 토글
    private void SetNodeState(StageNode node, NodeState state)
    {
        Button btn = _allNodes[node.floor, node.column];

        btn.RemoveFromClassList("node--available");
        btn.RemoveFromClassList("node--current");
        btn.RemoveFromClassList("node--visited");

        if (state != NodeState.Normal)
            btn.AddToClassList($"node--{state.ToString().ToLower()}");

        btn.SetEnabled(state == NodeState.Available);
    }
}
