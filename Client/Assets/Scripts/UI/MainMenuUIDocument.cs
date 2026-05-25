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

    [Header("Character Preview")]
    [SerializeField] private CharacterPreviewRenderer _previewRenderer;

    // === Private 변수 ===

    private UIDocument _document;

    // 패널
    private VisualElement _mainMenuPanel;
    private VisualElement _nameInputPanel;
    private VisualElement _roomListPanel;
    private VisualElement _createRoomPanel;
    private VisualElement _waitingRoomPanel;
    private VisualElement _upgradePanel;
    private VisualElement _characterSelectModal;

    // 메인 메뉴
    private Button _playButton;
    private Button _upgradeButton;

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
    // PlayerEntry: 행 컨테이너(클릭 캡쳐 대상) + 이름/캐릭터 라벨 + 현재 selected characterId
    private class PlayerEntry
    {
        public VisualElement Row;
        public Label CharacterLabel;
        public int CharacterId; // -1: 미선택
    }
    private readonly Dictionary<int, PlayerEntry> _playerEntries = new();
    private int  _maxPlayers;
    private bool _isRoomCreator;

    // 캐릭터 미리보기
    private VisualElement _charPreview;

    // 캐릭터 선택 모달 상태
    private ScrollView   _charSelectGrid;
    private Label        _charDetailName;
    private Label        _charDetailStats;
    private Label        _charDetailDesc;
    private Button       _charSelectCancelButton;
    private Button       _charSelectConfirmButton;
    private int          _focusedCharacterId = -1; // 모달에서 카드 클릭으로 선택 중인 ID (확정 전)
    private VisualElement _focusedCharacterCard;
    // characterId -> 카드 VisualElement 매핑 (포커스 스타일 토글용)
    private readonly Dictionary<int, VisualElement> _characterCards = new();

    // 강화 화면 - 1단계: 캐릭터 선택
    private VisualElement _upgradeCharSelectPanel;
    private ScrollView    _upgradeCharGrid;
    private Label         _upgradeSelectPointsLabel;
    private Button        _upgradeSelectBackButton;

    // 강화 화면 - 2단계: 강화 상세
    private Label      _upgradeGoldLabel;
    private Label      _upgradeDetailTitle;
    private ScrollView _upgradeScroll;
    private Button     _upgradeBackButton;

    // === Unity 생명주기 ===

    private void Awake()
    {
        Instance  = this;
        _document = GetComponent<UIDocument>();
    }

    private void OnEnable()
    {
        BindUI();
        // 진화 포인트 변동 시 강화 화면이 열려 있으면 자동 갱신
        // CurrencyPickup 등 외부에서 포인트가 변해도 UI가 stale 상태로 남지 않게 함
        PlayerEvents.OnEvolutionPointsChanged += OnEvolutionPointsChanged;
    }

    private void OnDisable()
    {
        UnbindUI();
        PlayerEvents.OnEvolutionPointsChanged -= OnEvolutionPointsChanged;
    }

    // === UI 바인딩 ===

    private void BindUI()
    {
        var root = _document.rootVisualElement;
        if (_styleSheet != null)
            root.styleSheets.Add(_styleSheet);

        _mainMenuPanel        = root.Q("main-menu-panel");
        _nameInputPanel       = root.Q("name-input-panel");
        _roomListPanel        = root.Q("room-list-panel");
        _createRoomPanel      = root.Q("create-room-panel");
        _waitingRoomPanel     = root.Q("waiting-room-panel");
        _upgradePanel         = root.Q("upgrade-panel");
        _characterSelectModal = root.Q("character-select-modal");

        _charSelectGrid          = root.Q<ScrollView>("char-select-grid");
        _charDetailName          = root.Q<Label>("char-detail-name");
        _charDetailStats         = root.Q<Label>("char-detail-stats");
        _charDetailDesc          = root.Q<Label>("char-detail-desc");
        _charSelectCancelButton  = root.Q<Button>("char-select-cancel");
        _charSelectConfirmButton = root.Q<Button>("char-select-confirm");

        _charPreview = root.Q<VisualElement>("char-preview");
        if (_previewRenderer != null && _charPreview != null)
        {
            _charPreview.style.backgroundImage = new StyleBackground(
                Background.FromRenderTexture(_previewRenderer.PreviewTexture));
        }

        _playButton    = root.Q<Button>("play-button");
        _upgradeButton = root.Q<Button>("upgrade-button");

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

        _upgradeCharSelectPanel   = root.Q("upgrade-character-select-panel");
        _upgradeCharGrid          = root.Q<ScrollView>("upgrade-character-grid");
        _upgradeSelectPointsLabel = root.Q<Label>("upgrade-select-points-label");
        _upgradeSelectBackButton  = root.Q<Button>("upgrade-select-back-button");

        _upgradeDetailTitle = root.Q<Label>("upgrade-detail-title");
        _upgradeGoldLabel   = root.Q<Label>("upgrade-gold-label");
        _upgradeScroll      = root.Q<ScrollView>("upgrade-scroll");
        _upgradeBackButton  = root.Q<Button>("upgrade-back-button");

        _playButton.clicked         += OnPlayClicked;
        _upgradeButton.clicked      += OnUpgradeClicked;
        _connectButton.clicked      += OnConnectClicked;
        _refreshButton.clicked      += OnRefreshClicked;
        _createRoomButton.clicked   += OnCreateRoomClicked;
        _cancelCreateButton.clicked += OnCancelCreateClicked;
        _confirmCreateButton.clicked+= OnConfirmCreateClicked;
        _leaveRoomButton.clicked    += OnLeaveRoomClicked;
        _startGameButton.clicked    += OnStartGameClicked;
        _charSelectCancelButton.clicked  += OnCharacterSelectCancel;
        _charSelectConfirmButton.clicked += OnCharacterSelectConfirm;
    }

    private void UnbindUI()
    {
        if (_upgradeButton      != null) _upgradeButton.clicked      -= OnUpgradeClicked;
        if (_upgradeBackButton  != null) _upgradeBackButton.clicked  -= OnUpgradeBackClicked;
        if (_playButton         != null) _playButton.clicked         -= OnPlayClicked;
        if (_connectButton      != null) _connectButton.clicked      -= OnConnectClicked;
        if (_refreshButton      != null) _refreshButton.clicked      -= OnRefreshClicked;
        if (_createRoomButton   != null) _createRoomButton.clicked   -= OnCreateRoomClicked;
        if (_cancelCreateButton != null) _cancelCreateButton.clicked -= OnCancelCreateClicked;
        if (_confirmCreateButton!= null) _confirmCreateButton.clicked-= OnConfirmCreateClicked;
        if (_leaveRoomButton    != null) _leaveRoomButton.clicked    -= OnLeaveRoomClicked;
        if (_startGameButton    != null) _startGameButton.clicked    -= OnStartGameClicked;
        if (_charSelectCancelButton  != null) _charSelectCancelButton.clicked  -= OnCharacterSelectCancel;
        if (_charSelectConfirmButton != null) _charSelectConfirmButton.clicked -= OnCharacterSelectConfirm;
    }

    // === 버튼 핸들러 ===

    private void OnPlayClicked()
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

    // 강화 클릭: 1단계(캐릭터 선택 그리드) 진입
    private void OnUpgradeClicked()
    {
        ShowPanel(_upgradeCharSelectPanel);
        RefreshUpgradeCharacterSelectPanel();

        // 1단계의 돌아가기 = 메인 메뉴, 2단계의 돌아가기 = 1단계
        _upgradeSelectBackButton.clicked += OnUpgradeSelectBackClicked;
        _upgradeBackButton.clicked       += OnUpgradeBackClicked;
    }

    // 2단계의 돌아가기: 캐릭터 선택 그리드(1단계)로 복귀
    private void OnUpgradeBackClicked()
    {
        ShowPanel(_upgradeCharSelectPanel);
        RefreshUpgradeCharacterSelectPanel();
    }

    // 1단계의 돌아가기: 메인 메뉴로 복귀, 핸들러 해제
    private void OnUpgradeSelectBackClicked()
    {
        _upgradeSelectBackButton.clicked -= OnUpgradeSelectBackClicked;
        _upgradeBackButton.clicked       -= OnUpgradeBackClicked;
        ShowPanel(_mainMenuPanel);
    }

    // === 패널 전환 ===

    private void ShowPanel(VisualElement target)
    {
        _mainMenuPanel.style.display          = DisplayStyle.None;
        _nameInputPanel.style.display         = DisplayStyle.None;
        _roomListPanel.style.display          = DisplayStyle.None;
        _createRoomPanel.style.display        = DisplayStyle.None;
        _waitingRoomPanel.style.display       = DisplayStyle.None;
        _upgradeCharSelectPanel.style.display = DisplayStyle.None;
        _upgradePanel.style.display           = DisplayStyle.None;

        // 모달은 패널 전환 시 항상 닫음
        if (_characterSelectModal != null)
            _characterSelectModal.style.display = DisplayStyle.None;

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
        _playerEntries.Clear();
        _playerScroll.Clear();

        // 신규 입장: 본인 캐릭터 미선택 상태로 시작 (서버 기본값 -1과 일치)
        AddPlayerEntry(NetworkManager.Instance.MyPlayerId, NetworkManager.Instance.MyPlayerName, -1);
        UpdatePlayerCount();
        RefreshStartGameButton();

        ShowPanel(_waitingRoomPanel);
    }

    // S_RoomJoined 수신 후 (방 참가): 대기방 전환, 전체 멤버 목록으로 세팅
    public void ShowWaitingRoomWithPlayers(RoomInfo room, List<PlayerInfo> players)
    {
        _maxPlayers = room.MaxPlayers;
        _roomNameLabel.text = room.RoomName;
        _playerEntries.Clear();
        _playerScroll.Clear();

        // PlayerInfo.CharacterId를 그대로 사용 (서버가 -1 또는 선택한 ID로 채워서 보냄)
        foreach (var p in players)
            AddPlayerEntry(p.PlayerId, p.PlayerName, p.CharacterId);
        UpdatePlayerCount();
        RefreshStartGameButton();

        ShowPanel(_waitingRoomPanel);
    }

    // S_PlayerJoined 수신 후: 새 플레이어 추가
    public void AddPlayerToWaitingRoom(PlayerInfo player)
    {
        AddPlayerEntry(player.PlayerId, player.PlayerName, player.CharacterId);
        UpdatePlayerCount();
        RefreshStartGameButton();
    }

    // S_PlayerLeft 수신 후: 플레이어 제거
    public void RemovePlayerFromWaitingRoom(int playerId)
    {
        if (!_playerEntries.TryGetValue(playerId, out var entry)) return;
        _playerScroll.Remove(entry.Row);
        _playerEntries.Remove(playerId);
        UpdatePlayerCount();
        RefreshStartGameButton();
    }

    // S_CreatorChanged 수신 후: 방장 변경 (방장 나갔을 때)
    public void UpdateCreatorStatus(int newCreatorId)
    {
        _isRoomCreator = (newCreatorId == NetworkManager.Instance.MyPlayerId);
        RefreshStartGameButton();
    }

    // S_PlayerCharacterSelected 수신 후: 해당 플레이어 라인의 캐릭터 표시 갱신
    public void UpdatePlayerCharacter(int playerId, int characterId)
    {
        if (!_playerEntries.TryGetValue(playerId, out var entry)) return;
        entry.CharacterId = characterId;
        UpdateCharacterLabel(entry, playerId == NetworkManager.Instance.MyPlayerId);
        RefreshStartGameButton();
    }

    // S_GameReady 수신 후: 씬 전환 (게임 서버 접속)
    public void OnGameReadyReceived()
    {
        _ = NetworkManager.Instance.ConnectToGameAsync();
    }

    // === 강화 화면 ===

    // 1단계: 강화할 캐릭터 그리드 갱신
    private void RefreshUpgradeCharacterSelectPanel()
    {
        RefreshUpgradeSelectPointsLabel();
        RefreshUpgradeCharacterGrid();
    }

    // 2단계: 선택된 캐릭터의 강화 상세 갱신
    private void RefreshUpgradeDetailPanel()
    {
        RefreshUpgradeDetailTitle();
        RefreshEvolutionPointsLabel();
        RefreshUpgradeList();
    }

    // 진화 포인트 변동 시 현재 보이는 패널만 갱신
    // CurrencyPickup 등 외부 변동에도 stale 상태가 안 남게 함
    private void OnEvolutionPointsChanged(int amount)
    {
        if (_upgradeCharSelectPanel != null && _upgradeCharSelectPanel.style.display.value == DisplayStyle.Flex)
        {
            RefreshUpgradeSelectPointsLabel();
            return;
        }
        if (_upgradePanel != null && _upgradePanel.style.display.value == DisplayStyle.Flex)
        {
            RefreshEvolutionPointsLabel();
            RefreshUpgradeList();
        }
    }

    private void RefreshEvolutionPointsLabel()
    {
        if (_upgradeGoldLabel == null) return;
        _upgradeGoldLabel.text = $"진화 포인트: {CurrencyHelper.GetEvolutionPoints()}";
    }

    private void RefreshUpgradeSelectPointsLabel()
    {
        if (_upgradeSelectPointsLabel == null) return;
        _upgradeSelectPointsLabel.text = $"진화 포인트: {CurrencyHelper.GetEvolutionPoints()}";
    }

    private void RefreshUpgradeDetailTitle()
    {
        if (_upgradeDetailTitle == null) return;

        var def = GameManager.Instance?.SelectedCharacter;
        _upgradeDetailTitle.text = def != null ? $"{def.displayName} 강화" : "캐릭터 강화";
    }

    // 1단계 그리드: 모달의 .char-select-card 스타일 재사용
    // 카드 클릭 → SelectCharacter (전역 + 영속화) → 2단계 패널 진입
    private void RefreshUpgradeCharacterGrid()
    {
        if (_upgradeCharGrid == null) return;
        _upgradeCharGrid.Clear();

        if (GameManager.Instance == null) return;

        var characters = GameManager.Instance.AllCharacters;
        if (characters == null || characters.Length == 0)
        {
            var empty = new Label("등록된 캐릭터가 없습니다.");
            empty.AddToClassList("player-entry");
            _upgradeCharGrid.Add(empty);
            return;
        }

        int selectedId = GameManager.Instance.SelectedCharacterId;
        foreach (var def in characters)
        {
            if (def == null) continue;
            int capturedId = def.characterId;

            var btn = new Button(() => OnUpgradeCharacterPicked(capturedId));
            btn.text = def.displayName;
            btn.AddToClassList("char-select-card");

            // 현재 전역 선택된 캐릭터를 강조 (재진입 시 직관적 표시)
            if (capturedId == selectedId)
                btn.AddToClassList("char-select-card--focused");

            _upgradeCharGrid.Add(btn);
        }
    }

    private void OnUpgradeCharacterPicked(int characterId)
    {
        // 전역 선택도 같이 변경 - 강화하는 캐릭터 = 인게임 캐릭터 (기존 동작 유지)
        GameManager.Instance?.SelectCharacter(characterId);

        ShowPanel(_upgradePanel);
        RefreshUpgradeDetailPanel();
    }

    private void RefreshUpgradeList()
    {
        if (_upgradeScroll == null) return;
        _upgradeScroll.Clear();

        if (UpgradeManager.Instance == null)
        {
            var warn = new Label("UpgradeManager가 씬에 없습니다.");
            warn.AddToClassList("player-entry");
            _upgradeScroll.Add(warn);
            return;
        }

        var defs = UpgradeManager.Instance.AllDefinitions;
        if (defs == null || defs.Length == 0)
        {
            var empty = new Label("강화 항목이 없습니다.");
            empty.AddToClassList("player-entry");
            _upgradeScroll.Add(empty);
            return;
        }

        foreach (var def in defs)
        {
            if (def == null) continue;
            _upgradeScroll.Add(BuildUpgradeEntry(def));
        }
    }

    private VisualElement BuildUpgradeEntry(UpgradeDefinition def)
    {
        int characterId = GameManager.Instance != null ? GameManager.Instance.SelectedCharacterId : 0;
        int level = UpgradeManager.Instance.GetLevel(characterId, def.type);
        bool isMaxed = level >= def.maxLevel;
        int cost = def.GetCost(level);

        var row = new VisualElement();
        row.AddToClassList("upgrade-entry");

        // 이름 + 설명 + 다음 레벨 미리보기
        var info = new VisualElement();
        info.AddToClassList("upgrade-entry__info");

        var nameLabel = new Label(def.displayName);
        nameLabel.AddToClassList("upgrade-entry__name");

        float currentTotal = def.GetTotalValue(level);
        string format = string.IsNullOrEmpty(def.descriptionFormat) ? "{0}" : def.descriptionFormat;
        var descLabel = new Label(string.Format(format, currentTotal));
        descLabel.AddToClassList("upgrade-entry__desc");

        info.Add(nameLabel);
        info.Add(descLabel);

        // 다음 레벨에서의 효과를 별도 라벨로 표시 - 강화 결정에 필요한 정보를 한눈에 제공
        if (!isMaxed)
        {
            float nextTotal = def.GetTotalValue(level + 1);
            var preview = new Label($"다음 → {string.Format(format, nextTotal)}");
            preview.AddToClassList("upgrade-entry__preview");
            info.Add(preview);
        }

        string levelText = isMaxed ? "MAX" : $"Lv.{level}/{def.maxLevel}";
        var levelLabel = new Label(levelText);
        levelLabel.AddToClassList("upgrade-entry__level");

        string btnText = isMaxed ? "최대" : $"강화 ({cost}EP)";
        var btn = new Button(() =>
        {
            if (UpgradeManager.Instance.TryUpgrade(characterId, def.type))
                RefreshUpgradeDetailPanel();
        });
        btn.text = btnText;
        btn.AddToClassList("upgrade-entry__button");
        if (isMaxed)
            btn.AddToClassList("upgrade-entry__button--maxed");
        btn.SetEnabled(!isMaxed && UpgradeManager.Instance.CanUpgrade(characterId, def.type));

        row.Add(info);
        row.Add(levelLabel);
        row.Add(btn);
        return row;
    }

    // === Private 헬퍼 ===

    private void AddPlayerEntry(int playerId, string playerName, int characterId)
    {
        if (_playerEntries.ContainsKey(playerId)) return;

        bool isSelf = playerId == NetworkManager.Instance.MyPlayerId;

        var row = new VisualElement();
        row.AddToClassList("player-entry");

        var nameLabel = new Label(isSelf ? $"{playerName} (나)" : playerName);
        nameLabel.AddToClassList("player-entry__name");
        row.Add(nameLabel);

        var charLabel = new Label();
        charLabel.AddToClassList("player-entry__character");
        row.Add(charLabel);

        var entry = new PlayerEntry { Row = row, CharacterLabel = charLabel, CharacterId = characterId };
        _playerEntries[playerId] = entry;

        UpdateCharacterLabel(entry, isSelf);

        // 본인 라인의 캐릭터 라벨을 클릭하면 모달 오픈
        // GameManager가 이미 SelectedCharacterId를 가지고 있으면 그 ID로 미리 포커스
        if (isSelf)
        {
            charLabel.RegisterCallback<ClickEvent>(_ => OpenCharacterSelectModal());
        }

        _playerScroll.Add(row);
    }

    private void UpdateCharacterLabel(PlayerEntry entry, bool isSelf)
    {
        bool ready = entry.CharacterId >= 0;
        string display;
        if (ready)
        {
            var def = GameManager.Instance != null
                ? GameManager.Instance.GetCharacterDefinition(entry.CharacterId)
                : null;
            display = def != null ? def.displayName : $"캐릭터 {entry.CharacterId}";
        }
        else
        {
            display = isSelf ? "캐릭터 선택" : "선택 중...";
        }
        entry.CharacterLabel.text = display;

        entry.CharacterLabel.EnableInClassList("player-entry__character--ready", ready);
        entry.CharacterLabel.EnableInClassList("player-entry__character--self", isSelf);
    }

    private void UpdatePlayerCount()
    {
        _roomCountLabel.text = $"{_playerEntries.Count} / {_maxPlayers}";
    }

    // 방장 + 모든 플레이어 캐릭터 선택 완료일 때만 게임시작 버튼 활성화
    // 서버도 동일 조건을 검증하지만 UX를 위해 클라에서도 게이팅
    private void RefreshStartGameButton()
    {
        if (_startGameButton == null) return;

        bool allReady = _playerEntries.Count > 0;
        foreach (var kv in _playerEntries)
        {
            if (kv.Value.CharacterId < 0) { allReady = false; break; }
        }

        _startGameButton.SetEnabled(_isRoomCreator && allReady);
    }


    // === 캐릭터 선택 모달 ===

    private void OpenCharacterSelectModal()
    {
        if (_characterSelectModal == null) return;

        // 본인 라인의 현재 선택 ID를 모달 포커스 기본값으로
        int myId = NetworkManager.Instance.MyPlayerId;
        int currentId = _playerEntries.TryGetValue(myId, out var entry) ? entry.CharacterId : -1;

        // 미선택 상태면 첫 번째 캐릭터를 기본 포커스로
        if (currentId < 0)
        {
            var characters = GameManager.Instance?.AllCharacters;
            if (characters != null && characters.Length > 0 && characters[0] != null)
                currentId = characters[0].characterId;
        }

        _focusedCharacterId = currentId;

        // UIDocument가 핫리로드 등으로 트리를 재빌드하면 이전에 쿼리한 _charPreview가
        // 고아 원소(orphaned element)가 돼서 backgroundImage가 렌더되지 않음
        // 모달을 열 때마다 다시 쿼리해서 재연결
        RebindPreviewBackground();

        BuildCharacterSelectGrid();
        RefreshCharacterDetail();

        _characterSelectModal.style.display = DisplayStyle.Flex;
    }

    private void RebindPreviewBackground()
    {
        if (_previewRenderer == null) return;
        var root = _document.rootVisualElement;
        _charPreview = root.Q<VisualElement>("char-preview");
        if (_charPreview == null) return;
        _charPreview.style.backgroundImage = new StyleBackground(
            Background.FromRenderTexture(_previewRenderer.PreviewTexture));
    }

    private void CloseCharacterSelectModal()
    {
        if (_characterSelectModal == null) return;
        _characterSelectModal.style.display = DisplayStyle.None;
        _focusedCharacterCard = null;
        _previewRenderer?.ClearPreview();
    }

    private void BuildCharacterSelectGrid()
    {
        if (_charSelectGrid == null) return;
        _charSelectGrid.Clear();
        _characterCards.Clear();
        _focusedCharacterCard = null;

        var characters = GameManager.Instance?.AllCharacters;
        if (characters == null || characters.Length == 0)
        {
            var empty = new Label("등록된 캐릭터가 없습니다.");
            empty.AddToClassList("player-entry");
            _charSelectGrid.Add(empty);
            return;
        }

        foreach (var def in characters)
        {
            if (def == null) continue;
            int capturedId = def.characterId;

            var btn = new Button(() => OnCharacterCardClicked(capturedId));
            btn.text = def.displayName;
            btn.AddToClassList("char-select-card");
            _charSelectGrid.Add(btn);
            _characterCards[capturedId] = btn;

            if (def.characterId == _focusedCharacterId)
            {
                btn.AddToClassList("char-select-card--focused");
                _focusedCharacterCard = btn;
            }
        }
    }

    private void OnCharacterCardClicked(int characterId)
    {
        _focusedCharacterId = characterId;

        if (_focusedCharacterCard != null)
            _focusedCharacterCard.RemoveFromClassList("char-select-card--focused");

        if (_characterCards.TryGetValue(characterId, out var card))
        {
            card.AddToClassList("char-select-card--focused");
            _focusedCharacterCard = card;
        }

        RefreshCharacterDetail();
    }

    private void RefreshCharacterDetail()
    {
        if (_charDetailName == null) return;

        var def = GameManager.Instance?.GetCharacterDefinition(_focusedCharacterId);
        if (def == null)
        {
            _charDetailName.text  = "캐릭터를 선택하세요";
            _charDetailStats.text = "";
            _charDetailDesc.text  = "";
            _charSelectConfirmButton?.SetEnabled(false);
            _previewRenderer?.ClearPreview();
            return;
        }

        _charDetailName.text  = def.displayName;
        _charDetailStats.text = $"체력 {def.baseMaxHealth}\n공격 +{def.baseAttackPower * 100f:0}%\n이속 {def.baseMoveSpeed}\n스태미나 {def.baseMaxStamina}";
        _charDetailDesc.text  = def.description;
        _charSelectConfirmButton?.SetEnabled(true);
        _previewRenderer?.Preview(def);
    }

    private void OnCharacterSelectCancel()
    {
        CloseCharacterSelectModal();
    }

    private void OnCharacterSelectConfirm()
    {
        if (_focusedCharacterId < 0) return;

        // 로컬 상태 즉시 반영 (S_PlayerCharacterSelected 응답 받기 전 UX 지연 방지)
        // 서버 응답이 오면 같은 값으로 한 번 더 반영되지만 멱등이므로 안전
        GameManager.Instance?.SelectCharacter(_focusedCharacterId);

        // 서버에 선택 통보
        NetworkManager.Instance.SendLobby(PacketId.CSelectCharacter, new C_SelectCharacter
        {
            CharacterId = _focusedCharacterId,
        });

        CloseCharacterSelectModal();
    }
}
