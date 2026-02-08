using UnityEngine;

public class StageManager : MonoBehaviour
{
    public static StageManager Instance { get; private set; }

 
    [Header("Stage Settings")]
    [SerializeField] private int _totalStages = 10;  // 총 스테이지 수

    
    private int _currentStage = 0;
    private bool _isStageActive = false;

    
    public int CurrentStage => _currentStage;
    public bool IsStageActive => _isStageActive;

    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnEnable()
    {
        StageEvents.OnAllEnemiesDefeated += HandleAllEnemiesDefeated;
    }

    private void OnDisable()
    {
        StageEvents.OnAllEnemiesDefeated -= HandleAllEnemiesDefeated;
    }

    

    // 게임 1트 시작 
    public void TryGame()
    {
        _currentStage = 0;
        StartStage(_currentStage);
    }

    // 특정 스테이지 시작
    public void StartStage(int stageNumber)
    {
        _currentStage = stageNumber;
        _isStageActive = true;

        Debug.Log($"[StageManager] 스테이지 {_currentStage + 1} 시작!");

        // 이벤트 발생
        StageEvents.StageStart(_currentStage);

        // 적 스폰
        SpawnManager.Instance.SpawnWaveForStage(_currentStage);
    }

    // 다음 스테이지로 진행
    public void NextStage()
    {
        _currentStage++;

        if (_currentStage >= _totalStages)
        {
            // 모든 스테이지 클리어
            GameClear();
            return;
        }

        StartStage(_currentStage);
    }

    

    private void HandleAllEnemiesDefeated()
    {
        if (!_isStageActive) return;

        _isStageActive = false;

        Debug.Log($"[StageManager] 스테이지 {_currentStage + 1} 클리어!");

        // 이벤트 발생
        StageEvents.StageClear(_currentStage);

        // 잠시 후 다음 스테이지 (연출용 딜레이)
        Invoke(nameof(NextStage), 2f);
    }

    private void GameClear()
    {
        Debug.Log("[StageManager] 모든 스테이지 클리어! 게임 클리어!");
        // TODO: 게임 클리어 처리 (결과 화면 등)
    }
}
