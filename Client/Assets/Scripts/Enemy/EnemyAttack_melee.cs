using UnityEngine;

// 근접 단일 타격 공격
// 쿨타임마다 공격 범위 내 플레이어에게 데미지 적용
public class EnemyAttack_Melee : EnemyAttack
{
    private float _lastAttackTime = -Mathf.Infinity;


    protected override void Awake()
    {
        base.Awake();
    }


    public override void TryAttack()
    {
        if (_enemy?.Data == null) return;
        if (Time.time - _lastAttackTime < _enemy.Data.attackCooldown) return;

        _lastAttackTime = Time.time;
        PerformAttack();
    }


    private void PerformAttack()
    {
        _enemy.Anim?.PlayAttack();

        // OverlapSphere로 범위 내 모든 콜라이더를 검사하는 이유:
        // 직접 참조 대신 물리 쿼리를 사용하면 멀티플레이나 여러 플레이어 지원 시 확장이 쉬움
        Collider[] hits = Physics.OverlapSphere(transform.position, _enemy.Data.attackRange);

        Collider playerHit = null;

        foreach (var hit in hits)
        {
            if (hit.transform.root.CompareTag("Player") || hit.CompareTag("Player"))
            {
                playerHit = hit;
                break;
            }
        }

        if (playerHit == null) return;

        // 호스트만 공격 판정 권한 보유 - 서버에 보고하면 서버가 S_PlayerDamaged 로 브로드캐스트
        if (!NetworkManager.Instance.IsHost) return;

        int targetPlayerId = GetHitPlayerId(playerHit.gameObject);
        NetworkManager.Instance.SendGame(GameProto.GamePacketId.CEnemyAttack, new GameProto.C_EnemyAttack
        {
            EnemyId        = _enemy.NetworkId,
            TargetPlayerId = targetPlayerId,
            Damage         = _enemy.Data.attackDamage,
        });
    }


    private int GetHitPlayerId(GameObject hitObj)
    {
        // 원격 플레이어라면 NetworkPlayer 컴포넌트에서 PlayerId 조회
        var np = hitObj.GetComponentInParent<NetworkPlayer>();
        if (np != null) return np.PlayerId;

        // 로컬 플레이어 (내 캐릭터)
        return NetworkManager.Instance.MyPlayerId;
    }


    private void OnDrawGizmos()
    {
        if (_enemy?.Data == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _enemy.Data.attackRange);
    }
}
