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

    GameOver,
    GameClear,
}


public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameState CurrentState { get; private set; }

    // 1차 개발 목표: Station 단일 맵에서 20분 웨이브 서바이벌
    // false로 바꾸면 기존 지하철 이동 루프로 복귀
    [SerializeField] private bool _survivalMode = true;
    public bool IsSurvivalMode => _survivalMode;


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
        ChangeState(GameState.Menu);
    }

    private void Update() { }


    // === Public 메서드 ===

    public void ChangeState(GameState newState)
    {
        CurrentState = newState;

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
        ChangeState(GameState.GameOver);
    }

    public void ClearGame()
    {
        ChangeState(GameState.GameClear);
    }
}
