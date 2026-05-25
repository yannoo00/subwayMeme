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

        float distance       = Vector3.Distance(transform.position, _player.position);
        float attackRange    = _enemy?.Data?.attackRange    ?? 2f;
        float detectionRange = _enemy?.Data?.detectionRange ?? 10f;

        if (distance <= attackRange)
            SetState(State.Attack);
        else if (distance <= detectionRange)
            SetState(State.Chase);
        else
            SetState(State.Idle);

        if (CurrentState == State.Chase)
            _enemy.Move?.MoveTo(_player.position);

        if (CurrentState == State.Attack)
            _enemy.Attack?.TryAttack();
    }
}
