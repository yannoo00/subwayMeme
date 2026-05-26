using UnityEngine;

// 기본 추적 AI
// 탐지 범위 안에 플레이어가 들어오면 추격 및 공격, 밖이면 대기
public class EnemyAI_basicChase : EnemyAI
{
    protected override void Think()
    {
        if (_player == null)
        {
            SetState(State.Idle);
            return;
        }

        float attackRange    = _enemy?.Data?.attackRange    ?? 2f;
        float detectionRange = _enemy.Data.detectionRange;

        float distToPlayer = GetDistanceTo(_player);

        if (distToPlayer > detectionRange)
        {
            SetState(State.Idle);
            return;
        }

        if (distToPlayer <= attackRange)
            SetState(State.Attack);
        else
            SetState(State.Chase);

        if (CurrentState == State.Chase)
            _enemy.Move?.MoveTo(_player.position);

        if (CurrentState == State.Attack)
            _enemy.Attack?.TryAttack();
    }
}
