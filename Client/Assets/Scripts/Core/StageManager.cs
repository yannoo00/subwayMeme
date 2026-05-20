using System.Collections;
using UnityEngine;


public class StageManager : MonoBehaviour
{
    public static StageManager Instance { get; private set; }

    [Header("Survival Mode")]
    [SerializeField] private WaveData _survivalWaveData;
    [SerializeField] private float _survivalDuration = 1200f; // 20분

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
    public void _TryGame(int seed)
    {
        SceneLoader.Instance.LoadStation(StartSurvivalMode);
    }


    // 서바이벌 모드: Station 씬에서 20분 웨이브 방어
    private void StartSurvivalMode()
    {
        if (_survivalWaveData != null)
            SpawnManager.Instance.StartWave(_survivalWaveData);
        else
            Debug.LogWarning("[StageManager] SurvivalMode: _survivalWaveData가 설정되지 않았습니다.");

        StageEvents.SurvivalStarted();
        _stayingCoroutine = StartCoroutine(SurvivalTimer(_survivalDuration));
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

        //남은 시간이 끝났다면
        GameManager.Instance.ClearGame();
    }


    // 모든 적 처치: 진행 트리거 아님. 추후 보너스 로직 연결 가능
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
