using System;
using System.Collections.Generic;
using Google.Protobuf;
using LobbyProto;
using UnityEngine;
using UnityEngine.UIElements;

// 메인 메뉴 + 로비 전체 UI 관리
// LobbyPacketHandler에서 Instance를 통해 호출하므로 static Instance 보유
// MainMenu 씬 오브젝트이므로 씬 전환 시 자동 소멸 (DontDestroyOnLoad 불필요)
public class MainMenuUIDocument : MonoBehaviour
{
    public static MainMenuUIDocument Instance { get; private set; }

    // === Inspector 변수 ===

    [Header("Style")]
    [SerializeField] private StyleSheet _styleSheet;

    // === Private 변수 ===

    private UIDocument _document;

    // 패널
    private VisualElement _mainMenuPanel;
    private VisualElement _nameInputPanel;
    private VisualElement _roomListPanel;
    private VisualElement _createRoomPanel;
    private VisualElement _waitingRoomPanel;

    // 메인 메뉴
    private Button _singleplayButton;
    private Button _multiplayButton;

    // 닉네임 입력
    private TextField _nameField;
    private Button    _connectButton;

    // 방 목록
    private ScrollView _roomScroll;
    private Button     _refreshButton;
    private Button     _createRoomButton;

    // 방 만들기
    private TextField _roomNameField;
    private Button    _cancelCreateButton;
    private Button    _confirmCreateButton;

    // 대기방
    private Label      _roomNameLabel;
    private Label      _roomCountLabel;
    private ScrollView _playerScroll;
    private Button     _leaveRoomButton;
    private Button     _startGameButton;

    // 대기방 상태
    private readonly Dictionary<int, Label> _playerLabels = new();
    private int  _maxPlayers;
    private bool _isRoomCreator;

    // === Unity 생명주기 ===

    private void Awake()
    {
        Instance  = this;
        _document = GetComponent<UIDocument>();
    }

    private void OnEnable()
    {
        BindUI();
    }

    private void OnDisable()
    {
        UnbindUI();
    }

    // === UI 바인딩 ===

    private void BindUI()
    {
        var root = _document.rootVisualElement;
        if (_styleSheet != null)
            root.styleSheets.Add(_styleSheet);

        _mainMenuPanel    = root.Q("main-menu-panel");
        _nameInputPanel   = root.Q("name-input-panel");
        _roomListPanel    = root.Q("room-list-panel");
        _createRoomPanel  = root.Q("create-room-panel");
        _waitingRoomPanel = root.Q("waiting-room-panel");

        _singleplayButton = root.Q<Button>("singleplay-button");
        _multiplayButton  = root.Q<Button>("multiplay-button");

        _nameField     = root.Q<TextField>("name-field");
        _connectButton = root.Q<Button>("connect-button");

        _roomScroll       = root.Q<ScrollView>("room-scroll");
        _refreshButton    = root.Q<Button>("refresh-button");
        _createRoomButton = root.Q<Button>("create-room-button");

        _roomNameField       = root.Q<TextField>("room-name-field");
        _cancelCreateButton  = root.Q<Button>("cancel-create-button");
        _confirmCreateButton = root.Q<Button>("confirm-create-button");

        _roomNameLabel   = root.Q<Label>("room-name-label");
        _roomCountLabel  = root.Q<Label>("room-count-label");
        _playerScroll    = root.Q<ScrollView>("player-scroll");
        _leaveRoomButton = root.Q<Button>("leave-room-button");
        _startGameButton = root.Q<Button>("start-game-button");

        _singleplayButton.clicked   += OnSingleplayClicked;
        _multiplayButton.clicked    += OnMultiplayClicked;
        _connectButton.clicked      += OnConnectClicked;
        _refreshButton.clicked      += OnRefreshClicked;
        _createRoomButton.clicked   += OnCreateRoomClicked;
        _cancelCreateButton.clicked += OnCancelCreateClicked;
        _confirmCreateButton.clicked+= OnConfirmCreateClicked;
        _leaveRoomButton.clicked    += OnLeaveRoomClicked;
        _startGameButton.clicked    += OnStartGameClicked;
    }

    private void UnbindUI()
    {
        if (_singleplayButton   != null) _singleplayButton.clicked   -= OnSingleplayClicked;
        if (_multiplayButton    != null) _multiplayButton.clicked    -= OnMultiplayClicked;
        if (_connectButton      != null) _connectButton.clicked      -= OnConnectClicked;
        if (_refreshButton      != null) _refreshButton.clicked      -= OnRefreshClicked;
        if (_createRoomButton   != null) _createRoomButton.clicked   -= OnCreateRoomClicked;
        if (_cancelCreateButton != null) _cancelCreateButton.clicked -= OnCancelCreateClicked;
        if (_confirmCreateButton!= null) _confirmCreateButton.clicked-= OnConfirmCreateClicked;
        if (_leaveRoomButton    != null) _leaveRoomButton.clicked    -= OnLeaveRoomClicked;
        if (_startGameButton    != null) _startGameButton.clicked    -= OnStartGameClicked;
    }

    // === 버튼 핸들러 ===

    private void OnSingleplayClicked()
    {
        _mainMenuPanel.style.display = DisplayStyle.None;
        int seed = UnityEngine.Random.Range(0, int.MaxValue);
        GameManager.Instance.StartGame(seed);
    }

    private void OnMultiplayClicked()
    {
        ShowPanel(_nameInputPanel);
    }

    // async void: UI 이벤트 핸들러에서 허용, 예외는 catch로 처리
    private async void OnConnectClicked()
    {
        string name = _nameField.value.Trim();
        if (string.IsNullOrEmpty(name)) return;

        _connectButton.SetEnabled(false);
        try
        {
            await NetworkManager.Instance.ConnectToLobbyAsync(name);
            // 성공 시 Handle_S_Connected가 ShowRoomListPanel() 호출
        }
        catch (Exception e)
        {
            Debug.LogError($"[LobbyUI] 접속 실패: {e.Message}");
            _connectButton.SetEnabled(true);
        }
    }

    private void OnRefreshClicked()
    {
        NetworkManager.Instance.SendLobby(PacketId.CGetRooms, new C_GetRooms());
    }

    private void OnCreateRoomClicked()
    {
        _roomNameField.value = "";
        ShowPanel(_createRoomPanel);
    }

    private void OnCancelCreateClicked()
    {
        ShowPanel(_roomListPanel);
    }

    private void OnConfirmCreateClicked()
    {
        string roomName = _roomNameField.value.Trim();
        if (string.IsNullOrEmpty(roomName)) return;

        _isRoomCreator = true; // 방 만들기 -> 내가 방장
        NetworkManager.Instance.SendLobby(PacketId.CCreateRoom, new C_CreateRoom
        {
            RoomName   = roomName,
            MaxPlayers = 4,
        });
    }

    private void OnLeaveRoomClicked()
    {
        NetworkManager.Instance.SendLobby(PacketId.CLeaveRoom, new C_LeaveRoom());        
        ShowRoomListPanel();
    }

    private void OnStartGameClicked()
    {
        _startGameButton.SetEnabled(false);
        NetworkManager.Instance.SendLobby(PacketId.CStartGame, new C_StartGame());
    }

    // === 패널 전환 ===

    private void ShowPanel(VisualElement target)
    {
        _mainMenuPanel.style.display    = DisplayStyle.None;
        _nameInputPanel.style.display   = DisplayStyle.None;
        _roomListPanel.style.display    = DisplayStyle.None;
        _createRoomPanel.style.display  = DisplayStyle.None;
        _waitingRoomPanel.style.display = DisplayStyle.None;
        target.style.display = DisplayStyle.Flex;
    }

    // === LobbyPacketHandler에서 호출하는 public 메서드 ===

    // S_Connected 수신 후: 방 목록 화면으로 전환 + 자동 갱신
    public void ShowRoomListPanel()
    {
        _connectButton.SetEnabled(true);
        ShowPanel(_roomListPanel);
        NetworkManager.Instance.SendLobby(PacketId.CGetRooms, new C_GetRooms());
    }

    // S_RoomList 수신 후: 방 목록 갱신
    public void RefreshRoomList(List<RoomInfo> rooms)
    {
        _roomScroll.Clear();

        if (rooms.Count == 0)
        {
            var empty = new Label("방이 없습니다.");
            empty.AddToClassList("player-entry");
            _roomScroll.Add(empty);
            return;
        }

        foreach (var room in rooms)
        {
            int capturedId = room.RoomId;
            var btn = new Button(() =>
            {
                _isRoomCreator = false; // 참가자는 방장 아님
                NetworkManager.Instance.SendLobby(PacketId.CJoinRoom, new C_JoinRoom { RoomId = capturedId });
            });
            btn.text = $"{room.RoomName}  {room.CurPlayers} / {room.MaxPlayers}";
            btn.AddToClassList("room-entry-button");
            _roomScroll.Add(btn);
        }
    }

    // S_RoomCreated 수신 후 (방 생성): 대기방 전환, 본인만 목록에 추가
    public void ShowWaitingRoom(RoomInfo room)
    {
        _maxPlayers = room.MaxPlayers;
        _roomNameLabel.text = room.RoomName;
        _playerLabels.Clear();
        _playerScroll.Clear();

        AddPlayerEntry(NetworkManager.Instance.MyPlayerId, NetworkManager.Instance.MyPlayerName);
        UpdatePlayerCount();

        _startGameButton.SetEnabled(_isRoomCreator);
        ShowPanel(_waitingRoomPanel);
    }

    // S_RoomJoined 수신 후 (방 참가): 대기방 전환, 전체 멤버 목록으로 세팅
    public void ShowWaitingRoomWithPlayers(RoomInfo room, List<PlayerInfo> players)
    {
        _maxPlayers = room.MaxPlayers;
        _roomNameLabel.text = room.RoomName;
        _playerLabels.Clear();
        _playerScroll.Clear();

        foreach (var p in players)
            AddPlayerEntry(p.PlayerId, p.PlayerName);
        UpdatePlayerCount();

        _startGameButton.SetEnabled(_isRoomCreator);
        ShowPanel(_waitingRoomPanel);
    }

    // S_PlayerJoined 수신 후: 새 플레이어 추가
    public void AddPlayerToWaitingRoom(PlayerInfo player)
    {
        AddPlayerEntry(player.PlayerId, player.PlayerName);
        UpdatePlayerCount();
    }

    // S_PlayerLeft 수신 후: 플레이어 제거
    public void RemovePlayerFromWaitingRoom(int playerId)
    {
        if (!_playerLabels.TryGetValue(playerId, out var label)) return;
        _playerScroll.Remove(label);
        _playerLabels.Remove(playerId);
        UpdatePlayerCount();
    }

    // S_CreatorChanged 수신 후: 방장 변경 (방장 나갔을 때)
    public void UpdateCreatorStatus(int newCreatorId)
    {
        _isRoomCreator = (newCreatorId == NetworkManager.Instance.MyPlayerId);
        _startGameButton.SetEnabled(_isRoomCreator);
    }

    // S_GameReady 수신 후: 씬 전환 (게임 서버 접속)
    public void OnGameReadyReceived()
    {
        _ = NetworkManager.Instance.ConnectToGameAsync();
    }

    // === Private 헬퍼 ===

    private void AddPlayerEntry(int playerId, string playerName)
    {
        if (_playerLabels.ContainsKey(playerId)) return;
        var label = new Label(playerName);
        label.AddToClassList("player-entry");
        _playerScroll.Add(label);
        _playerLabels[playerId] = label;
    }

    private void UpdatePlayerCount()
    {
        _roomCountLabel.text = $"{_playerLabels.Count} / {_maxPlayers}";
    }
}
