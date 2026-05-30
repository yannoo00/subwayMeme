using UnityEngine;

// 공격 방식 추상 클래스
// EnemyAI가 Attack 상태일 때 TryAttack()을 호출하면, 구체 클래스가 실제 공격 처리
// 구체 클래스: MeleeAttack, ProjectileAttack 등
public abstract class EnemyAttack : MonoBehaviour
{
    protected Enemy _enemy;

    protected virtual void Awake()
    {
    }
        _enemy = GetComponent<Enemy>();


    // EnemyAI가 Attack 상태일 때 매 프레임 호출
    // 쿨타임 및 실제 공격 처리는 구체 클래스가 담당
    public abstract void TryAttack();
}
