using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyMove_navMesh : EnemyMove
{
    private NavMeshAgent _agent;


    protected override void Awake()
    {
        base.Awake();
        _agent = GetComponent<NavMeshAgent>();
    }


    private void Start()
    {
        // Awake 실행 순서가 보장되지 않으므로 Start에서 속도 적용
        if (_enemy?.Data != null)
            _agent.speed = _enemy.Data.moveSpeed;
    }


    public override void MoveTo(Vector3 destination)
    {
        _agent.isStopped = false;
        _agent.SetDestination(destination);
    }


    public override void Stop()
    {
        _agent.isStopped = true;
    }
}
