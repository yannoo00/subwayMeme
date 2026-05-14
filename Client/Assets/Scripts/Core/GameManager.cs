using UnityEngine;
using UnityEngine.InputSystem;


// 패킷 처리의 기준이 되는 상태
public enum GameState
{
    Menu,           // 메인메뉴, 로비

    EnteringGame,   // 게임씬 로드 완료 ~ S_GameStart 수신 전 (C_EnterGame, S_EnterGame, C_Ready 처리)

    SceneLoading,   // 씬 전환 중 (S_AllBoarded/S_AllExited ~ 씬 로드 완료)
                    // 이 구간에서 오는 게임 패킷은 전부 무시

    Playing,        // 인게임 (이동, 전투, 타이머 등 동기화 패킷 처리)
}


public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameState CurrentState { get; private set; }

    // 게임오버/클리어 중복 처리 방지 가드
    // 발전기 파괴와 전원 사망이 같은 프레임에 발생해도 결과는 한 번만 확정되어야 함
    private bool _isGameEnded;

    // 1차 개발 목표: Station 단일 맵에서 20분 웨이브 서바이벌
    // false로 바꾸면 기존 지하철 이동 루프로 복귀
    [SerializeField] private bool _survivalMode = true;
    public bool IsSurvivalMode => _survivalMode;

    [Header("캐릭터")]
    [SerializeField] private CharacterDefinition[] _characters;

    // 강화 화면 / 인게임 스폰에서 사용할 현재 선택된 캐릭터 ID
    // 1차 개발에서는 캐릭터가 1종이라 0 고정이지만, 추후 캐릭터 선택 UI에서 SelectCharacter를 호출해 변경
    public int SelectedCharacterId { get; private set; } = 0;

    public CharacterDefinition[] AllCharacters => _characters;
    public CharacterDefinition SelectedCharacter => GetCharacterDefinition(SelectedCharacterId);


    // === 생명주기 ===

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        // SaveData에서 마지막 선택 캐릭터 복원
        // SaveManager.Awake가 먼저 실행되어 Current가 로드된 상태라고 가정 (둘 다 Persistent 씬)
        // -1 또는 정의되지 않은 ID면 기본값 0 유지
        var save = SaveManager.Instance?.Current;
        if (save != null && save.lastSelectedCharacterId >= 0
            && GetCharacterDefinition(save.lastSelectedCharacterId) != null)
        {
            SelectedCharacterId = save.lastSelectedCharacterId;
        }

        ChangeState(GameState.Menu);
    }

    private void Update() { }


    // === Public 메서드 ===


    public void ChangeState(GameState newState)
    {
        CurrentState = newState;

        // 여기서는 마우스를 조작하지 않는것이 좋을듯 함(수정 예정)
        switch (newState)
        {

            case GameState.Playing:
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                break;

            
            default:
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                break;
        }
    }


    // === 게임 흐름 ===

    // S_GameReady 수신 시 호출: 게임씬 로드 시작 전 상태 전환
    public void OnEnteringGame()
    {
        ChangeState(GameState.EnteringGame);
    }

    // S_GameStart 수신 시 호출: 맵 생성 후 노선도 UI 표시
    public void StartGame(int seed)
    {
        Time.timeScale = 1f;
        _isGameEnded = false;
        ChangeState(GameState.Playing);

        StageManager.Instance._TryGame(seed);
    }

    // S_AllBoarded / S_AllExited 수신 시 호출: 씬 전환 시작
    public void OnSceneTransitionStart()
    {
        ChangeState(GameState.SceneLoading);
    }

    // 씬 로드 완료 콜백에서 호출
    public void OnSceneTransitionEnd()
    {
        ChangeState(GameState.Playing);
    }

    public void PauseGame()
    {
        if (CurrentState == GameState.Playing)
            ChangeState(GameState.Menu);
    }

    public void ResumeGame()
    {
        if (CurrentState == GameState.Menu)
        {
            Time.timeScale = 1f;
            ChangeState(GameState.Playing);
        }
    }

    public void EndGame()
    {
        if (_isGameEnded) return;
        _isGameEnded = true;

        StopGameplayRoutines();
        GameEvents.GameOver();
    }

    public void ClearGame()
    {
        if (_isGameEnded) return;
        _isGameEnded = true;

        StopGameplayRoutines();
        GameEvents.GameClear();
    }

    // 게임 종료(오버/클리어) 시 진행 중이던 타이머/웨이브 스폰을 일괄 정지
    private void StopGameplayRoutines()
    {
        StageManager.Instance?.StopAllStageRoutines();
        SpawnManager.Instance?.StopAllWaves();
    }


    // === 캐릭터 선택 ===

    // 강화 화면이나 캐릭터 선택 UI에서 호출
    // SaveData에 슬롯이 없으면 자동 생성 (신규 캐릭터 첫 선택 시)
    public void SelectCharacter(int characterId)
    {
        if (GetCharacterDefinition(characterId) == null)
        {
            Debug.LogWarning($"[GameManager] 알 수 없는 characterId={characterId}");
            return;
        }

        SelectedCharacterId = characterId;

        // SaveData 슬롯 자동 생성 + 마지막 선택 ID 영속화
        var save = SaveManager.Instance?.Current;
        if (save != null)
        {
            save.GetOrCreateCharacter(characterId);
            save.lastSelectedCharacterId = characterId;
            SaveManager.Instance.Save();
        }
    }

    public CharacterDefinition GetCharacterDefinition(int characterId)
    {
        if (_characters == null) return null;
        foreach (var c in _characters)
            if (c != null && c.characterId == characterId) return c;
        return null;
    }
}
