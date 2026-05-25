using UnityEngine;

// 기본 추적 AI
// 평소에는 발전기를 무조건 추격
// 탐지 범위 안에 플레이어가 들어오면 플레이어 우선 추격 및 공격
public class EnemyAI_basicChase : EnemyAI
{
    private Transform _generator;

    protected override void Start()
    {
        base.Start();

        GameObject generatorObj = GameObject.FindGameObjectWithTag("Generator");
        if (generatorObj != null)
            _generator = generatorObj.transform;
    }

    protected override void Think()
    {
        float attackRange    = _enemy?.Data?.attackRange    ?? 2f;
        float detectionRange = _enemy?.Data?.detectionRange ?? 10f;

        // 탐지 범위 안에 플레이어가 있으면 플레이어 우선
        bool playerInRange = _player != null &&
                             Vector3.Distance(transform.position, _player.position) <= detectionRange;

        if (playerInRange)
        {
            float distToPlayer = Vector3.Distance(transform.position, _player.position);

            if (distToPlayer <= attackRange)
                SetState(State.Attack);
            else
                SetState(State.Chase);

            if (CurrentState == State.Chase)
                _enemy.Move?.MoveTo(_player.position);

            if (CurrentState == State.Attack)
                _enemy.Attack?.TryAttack();

            return;
        }

        // 플레이어가 범위 밖이면 발전기 무조건 추격
        if (_generator != null)
        {
            float distToGen = Vector3.Distance(transform.position, _generator.position);

            if (distToGen <= attackRange)
                SetState(State.Attack);
            else
                SetState(State.Chase);

            if (CurrentState == State.Chase)
                _enemy.Move?.MoveTo(_generator.position);

            if (CurrentState == State.Attack)
                _enemy.Attack?.TryAttack();

            return;
        }

        SetState(State.Idle);
    }
}
