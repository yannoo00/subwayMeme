using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(EnemyStats))]
public class EnemyAI : MonoBehaviour
{
    public enum State { Idle, Chase, Attack, Dead }

    [Header("Detection")]
    [SerializeField] private float _detectionRange = 10f;
    [SerializeField] private float _attackRange = 2f;

    [Header("Death")]
    // 사망 애니메이션이 끝난 후 오브젝트를 제거하기까지의 대기 시간
    [SerializeField] private float _destroyDelay = 2f;

    private NavMeshAgent _agent;
    private EnemyStats _stats;
    private Animator _animator;
    private EnemyAttack _attack;
    private Transform _player;

    private State _currentState = State.Idle;

    public State CurrentState => _currentState;


    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _stats = GetComponent<EnemyStats>();
        _animator = GetComponentInChildren<Animator>();
        // EnemyAttack은 선택적 컴포넌트 (나중에 추가)
        _attack = GetComponent<EnemyAttack>();
    }


    private void Start()
    {
        // Player 태그로 플레이어 탐색
        // DontDestroyOnLoad 씬에 있는 플레이어도 FindWithTag로 찾을 수 있음
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            _player = playerObj.transform;
        else
            Debug.LogWarning("[EnemyAI] Player 태그 오브젝트를 찾지 못했습니다.");
    }


    private void Update()
    {
        if (_currentState == State.Dead) return;

        if (_player == null)
        {
            SetState(State.Idle);
            return;
        }

        float distance = Vector3.Distance(transform.position, _player.position);

        if (distance <= _attackRange)
            SetState(State.Attack);
        else if (distance <= _detectionRange)
            SetState(State.Chase);
        else
            SetState(State.Idle);

        // Attack 상태일 때 매 프레임 공격 시도
        // EnemyAttack 내부에서 쿨타임을 관리하므로 매 프레임 호출해도 안전
        if (_currentState == State.Attack)
            _attack?.TryAttack();
    }


    private void FixedUpdate()
    {
        // Chase 상태일 때만 NavMeshAgent 목적지 갱신
        // FixedUpdate에서 갱신하는 이유: 물리 연산 주기와 맞춰 NavMesh 부하를 줄이기 위함
        if (_currentState == State.Chase && _player != null)
            _agent.SetDestination(_player.position);
    }


    private void SetState(State newState)
    {
        if (_currentState == newState) return;
        _currentState = newState;

        switch (_currentState)
        {
            case State.Idle:
                _agent.isStopped = true;
                _animator?.SetBool(AnimatorParams.IsMoving, false);
                break;

            case State.Chase:
                _agent.isStopped = false;
                _animator?.SetBool(AnimatorParams.IsMoving, true);
                break;

            case State.Attack:
                _agent.isStopped = true;
                _animator?.SetBool(AnimatorParams.IsMoving, false);
                break;
        }
    }


    // EnemyStats.Die()에서 호출됨
    // 사망 애니메이션 재생 후 오브젝트 제거까지 담당
    public void OnDeath()
    {
        if (_currentState == State.Dead) return;

        _currentState = State.Dead;
        _agent.isStopped = true;
        // NavMeshAgent를 비활성화해야 이후 Transform 조작이나 충돌 문제가 없음
        _agent.enabled = false;
        _animator?.SetBool(AnimatorParams.IsDead, true);

        StartCoroutine(DestroyAfterDelay());
    }


    private IEnumerator DestroyAfterDelay()
    {
        yield return new WaitForSeconds(_destroyDelay);
        Destroy(gameObject);
    }
}
