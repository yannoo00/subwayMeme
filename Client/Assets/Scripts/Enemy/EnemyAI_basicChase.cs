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
        // detectionRange는 플레이어 추적에만 사용 - 발전기는 거리 무관 무조건 추적
        float detectionRange = _enemy.Data.detectionRange;

        // 플레이어가 탐지 범위 안이면 플레이어 우선
        if (_player != null)
        {
            float distToPlayer = GetDistanceTo(_player);
            if (distToPlayer <= detectionRange)
            {
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
        }

        // 플레이어가 범위 밖이면 발전기 무조건 추격 (거리 무관)
        if (_generator != null)
        {
            float distToGen = GetDistanceTo(_generator);

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
