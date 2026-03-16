using UnityEngine;

// 적 오브젝트의 중앙 조율자
// EnemyData를 읽어 각 컴포넌트에 값을 주입하고
// 외부(SpawnManager 등)에서는 이 클래스만 바라보면 됨
[RequireComponent(typeof(EnemyStats))]
[RequireComponent(typeof(EnemyAI))]
[RequireComponent(typeof(EnemyAttack))]
public class EnemyController : MonoBehaviour
{
    [SerializeField] private EnemyData _data;

    private EnemyStats  _stats;
    private EnemyAI     _ai;
    private EnemyAttack _attack;


    private void Awake()
    {
        _stats  = GetComponent<EnemyStats>();
        _ai     = GetComponent<EnemyAI>();
        _attack = GetComponent<EnemyAttack>();

        if (_data != null)
            ApplyData();
    }



    private void ApplyData()
    {
        _stats?.Initialize(_data.maxHealth);
        _ai?.Initialize(_data.moveSpeed, _data.detectionRange, _data.attackRange);
        _attack?.Initialize(_data.attackDamage, _data.attackRange, _data.attackCooldown);
    }
}
