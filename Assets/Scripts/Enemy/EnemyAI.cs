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
    [SerializeField] private float _destroyDelay = 2f;

    protected Enemy         _enemy;
    protected EnemyMove     _move;
    protected EnemyAttack   _attack;
    protected EnemyAnimator _anim;

    protected Transform _player;

    private State _currentState = State.Idle;
    public  State CurrentState  => _currentState;


    protected virtual void Awake()
    {
        _enemy  = GetComponent<Enemy>();
        _move   = GetComponent<EnemyMove>();
        _attack = GetComponent<EnemyAttack>();
        _anim   = GetComponent<EnemyAnimator>();
    }


    protected virtual void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            _player = playerObj.transform;
        else
            Debug.LogWarning("[EnemyAI] Player 태그 오브젝트를 찾지 못했습니다.");
    }


    private void Update()
    {
        if (_currentState == State.Dead) return;
        Think();
    }


    // 매 프레임 호출. 자식 클래스가 거리/조건에 따라 SetState, Move, Attack을 결정
    protected abstract void Think();


    protected void SetState(State newState)
    {
        if (_currentState == newState) return;
        _currentState = newState;

        switch (_currentState)
        {
            case State.Idle:
                _move?.Stop();
                _anim?.PlayIdle();
                break;

            case State.Chase:
                _anim?.PlayMove();
                break;

            case State.Attack:
                _move?.Stop();
                _anim?.PlayAttack();
                break;
        }
    }


    // Enemy.Die()에서 호출됨
    public void OnDeath()
    {
        if (_currentState == State.Dead) return;

        _currentState = State.Dead;
        _move?.Stop();
        _anim?.PlayDeath();

        StartCoroutine(DestroyAfterDelay());
    }


    private IEnumerator DestroyAfterDelay()
    {
        yield return new WaitForSeconds(_destroyDelay);
        Destroy(gameObject);
    }
}
