using UnityEngine;

// 플레이어만 추적하는 AI
// 탐지 범위 안에 들어오면 추격, 공격 범위 안에 들어오면 공격
public class EnemyAI_playerChase : EnemyAI
{
    protected override void Think()
    {
        if (_player == null)
        {
            SetState(State.Idle);
            return;
        }

        float distance    = GetDistanceTo(_player);
        float attackRange = _enemy?.Data?.attackRange ?? 2f;

        if (distance <= attackRange)
            SetState(State.Attack);
        else if (distance <= _enemy.Data.detectionRange)
            SetState(State.Chase);
        else
            SetState(State.Idle);

        if (CurrentState == State.Chase)
            _enemy.Move?.MoveTo(_player.position);

        if (CurrentState == State.Attack)
            _enemy.Attack?.TryAttack();
    }
}
