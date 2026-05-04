using UnityEngine;

// 발전기를 우선 추적하는 AI
// 발전기가 없거나 파괴된 경우 플레이어 추적으로 전환
public class EnemyAI_generatorChase : EnemyAI
{
    private Transform _generator;

    protected override void Start()
    {
        base.Start();

        GameObject generatorObj = GameObject.FindGameObjectWithTag("Generator");
        if (generatorObj != null)
            _generator = generatorObj.transform;
        else
            Debug.LogWarning("[EnemyAI_generatorChase] Generator 태그 오브젝트를 찾지 못했습니다.");
    }

    protected override void Think()
    {
        Transform target = GetTarget();

        if (target == null)
        {
            SetState(State.Idle);
            return;
        }

        float distance       = Vector3.Distance(transform.position, target.position);
        float attackRange    = _enemy?.Data?.attackRange    ?? 2f;
        float detectionRange = _enemy?.Data?.detectionRange ?? 10f;

        if (distance <= attackRange)
            SetState(State.Attack);
        else if (distance <= detectionRange)
            SetState(State.Chase);
        else
            SetState(State.Idle);

        if (CurrentState == State.Chase)
            _enemy.Move?.MoveTo(target.position);

        if (CurrentState == State.Attack)
            _enemy.Attack?.TryAttack();
    }

    // 발전기가 살아있으면 발전기, 아니면 플레이어
    private Transform GetTarget()
    {
        if (_generator != null)
        {
            var gen = _generator.GetComponent<IDamageable>();
            if (gen != null && gen.IsAlive)
                return _generator;
        }

        return _player;
    }
}
