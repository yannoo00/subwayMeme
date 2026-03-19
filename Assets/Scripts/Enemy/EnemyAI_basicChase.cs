using UnityEngine;

// 기본 추적 AI
// 탐지 범위 안에 들어오면 플레이어를 추적하고, 공격 범위 안에 들어오면 공격
// 여기서 Attack과 Move를 사용해서 상황에 맞게 이동, 공격
// State 조정
public class EnemyAI_basicChase : EnemyAI
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

        // EnemyAttack 내부에서 쿨타임을 관리하므로 매 프레임 호출해도 안전
        if (CurrentState == State.Attack)
            _enemy.Attack?.TryAttack();
    }
}
