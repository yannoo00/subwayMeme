using System.Collections;
using UnityEngine;


public class StageManager : MonoBehaviour
{
    public static StageManager Instance { get; private set; }

    [Header("Survival Mode")]
    [SerializeField] private WaveData _survivalWaveData;
    [SerializeField] private float _survivalDuration = 120f;

    // 서버가 S_GameStart에 실어 보낸 실제 게임 시간 (C_Ready에서 호스트가 전달한 값)
    private float _activeDuration;

    public float SurvivalDuration => _survivalDuration;

    private Coroutine _stayingCoroutine;


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
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
    // GameManager에서만 사용하고, 직접 호출되는 일은 없어야 함
    public void _TryGame(int seed, int survivalDurationSecs)
    {
        _activeDuration = survivalDurationSecs > 0 ? survivalDurationSecs : _survivalDuration;
        SceneLoader.Instance.LoadStation(StartSurvivalMode);
    }

    private void StartSurvivalMode()
    {
        if (_survivalWaveData != null)
            SpawnManager.Instance.StartWave(_survivalWaveData);

        StageEvents.SurvivalStarted();
        _stayingCoroutine = StartCoroutine(SurvivalTimer(_activeDuration));
    }


    private IEnumerator SurvivalTimer(float duration)
    {
        float remaining = duration;
        StageEvents.TimerTick(remaining, duration);

        while (remaining > 0f)
        {
            yield return new WaitForSeconds(1f);
            remaining -= 1f;
            StageEvents.TimerTick(remaining, duration);
        }

        // 타이머 UI 표시용으로만 실행. 클리어 판정은 서버가 S_GameClear 전송으로 처리.
    }


    private void HandleAllEnemiesDefeated()
    {
        Debug.Log("[StageManager] 모든 적 처치!");
    }


    // 게임 종료(오버/클리어) 시 진행 중이던 모든 스테이지 타이머를 정지
    public void StopAllStageRoutines()
    {
        if (_stayingCoroutine != null) StopCoroutine(_stayingCoroutine);
        _stayingCoroutine = null;
    }
}
