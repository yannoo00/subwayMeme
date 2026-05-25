using System.Collections;
using UnityEngine;

// 적 AI 추상 클래스
// 상태 관리와 사망 처리는 공통 제공
// Think()를 구현해서 각 AI가 매 프레임 상태를 결정
// 구체 클래스: BasicChaseAI, PatrolAI, BossAI 등
public abstract class EnemyAI : MonoBehaviour
{
    public enum State { Idle, Chase, Attack, Dead }

    [Header("Death")]
    private float _destroyDelay = 2f;

    // 타겟 재계산 주기. 매 Think 마다 GetNearestAlivePlayer 를 돌리면 CPU 부담이 있어
    // 5초 주기로만 갱신하고, 타겟이 죽었을 때만 OnAnyPlayerDied 구독으로 즉시 갱신
    private const float RETARGET_INTERVAL = 5f;

    protected Enemy     _enemy;
    protected Transform _player;

    private Coroutine _retargetCoroutine;

    private State _currentState = State.Idle;
    public  State CurrentState  => _currentState;


    protected virtual void Awake()
    {
        _enemy = GetComponent<Enemy>();
    }


    protected virtual void Start()
    {
        // FindGameObjectWithTag("Player") 는 RemotePlayer 도 같은 태그라 잘못 잡힐 수 있어 사용 안 함.
        // PlayerRegistry 가 LocalPlayer 와 원격 플레이어들을 모두 관리하므로 그쪽에 위임.
        UpdateTarget();
        _retargetCoroutine = StartCoroutine(RetargetLoop());
    }


    protected virtual void OnEnable()
    {
        // 자기 타겟이 죽었을 때만 즉시 재계산하기 위해 구독
        PlayerEvents.OnAnyPlayerDied += HandleAnyPlayerDied;
    }


    protected virtual void OnDisable()
    {
        PlayerEvents.OnAnyPlayerDied -= HandleAnyPlayerDied;
    }


    private void Update()
    {
        if (_currentState == State.Dead) return;
        Think();
    }


    // 5초 주기로 가장 가까운 생존 플레이어로 타겟 갱신
    private IEnumerator RetargetLoop()
    {
        // WaitForSeconds 인스턴스 재사용으로 매 yield 마다 GC 할당 회피
        var wait = new WaitForSeconds(RETARGET_INTERVAL);
        while (true)
        {
            yield return wait;
            UpdateTarget();
        }
    }


    private void UpdateTarget()
    {
        if (PlayerRegistry.Instance == null) return;
        _player = PlayerRegistry.Instance.GetNearestAlivePlayer(transform.position);
    }


    // PlayerRegistry 가 사망/퇴장 시 호출. 내 현재 타겟이 죽었을 때만 즉시 다음 타겟으로 교체
    private void HandleAnyPlayerDied(Transform deadPlayer)
    {
        if (_player == deadPlayer) UpdateTarget();
    }


    // 매 프레임 호출. 자식 클래스가 거리/조건에 따라 SetState, Move, Attack을 결정
    protected abstract void Think();


    // 타겟까지의 거리를 콜라이더 표면 기준으로 계산
    // pivot 거리는 발전기처럼 큰 모델에선 외벽 위에 좀비가 붙어 있어도 큰 값이 나와
    // attackRange 안으로 못 들어가는 문제가 생긴다. 콜라이더 표면 거리를 쓰면 모델 크기와 무관.
    // 콜라이더가 없으면 pivot 거리로 폴백.
    protected float GetDistanceTo(Transform target)
    {
        if (target == null) return float.MaxValue;

        if (target.TryGetComponent<Collider>(out var col))
        {
            Vector3 closest = col.ClosestPoint(transform.position);
            return Vector3.Distance(transform.position, closest);
        }

        return Vector3.Distance(transform.position, target.position);
    }


    protected void SetState(State newState)
    {
        if (_currentState == newState) return;
        _currentState = newState;

        switch (_currentState)
        {
            case State.Idle:
                _enemy.Move?.Stop();
                _enemy.Anim?.PlayIdle();
                break;

            case State.Chase:
                _enemy.Anim?.PlayMove();
                break;

            case State.Attack:
                _enemy.Move?.Stop();
                _enemy.Anim?.PlayAttack();
                break;
        }
    }


    // Enemy.Die()에서 호출됨
    public void OnDeath()
    {
        if (_currentState == State.Dead) return;

        _currentState = State.Dead;

        // 사망 후 DestroyDelay 동안 재타겟 계산은 의미 없음 - 명시적으로 멈춰 부하 절약
        if (_retargetCoroutine != null)
        {
            StopCoroutine(_retargetCoroutine);
            _retargetCoroutine = null;
        }

        _enemy.Move?.Stop();
        _enemy.Anim?.PlayDeath();

        StartCoroutine(DestroyAfterDelay());
    }


    private IEnumerator DestroyAfterDelay()
    {
        yield return new WaitForSeconds(_destroyDelay);
        Destroy(gameObject);
    }
}
