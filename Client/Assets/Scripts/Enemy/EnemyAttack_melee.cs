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

        Debug.Log($"[MeleeAttack] OverlapSphere 감지된 콜라이더 수: {hits.Length}");

        // 발전기 우선 - 두 종류 다 범위 안이면 발전기를 때리는 게 일관됨
        // (PlayerChase 타입이 아닌 좀비는 어차피 발전기가 본 목표)
        Collider generatorHit = null;
        Collider playerHit    = null;

        foreach (var hit in hits)
        {
            Debug.Log($"  - {hit.gameObject.name} / 태그: {hit.tag}");

            if (generatorHit == null &&
                (hit.transform.root.CompareTag("Generator") || hit.CompareTag("Generator")))
            {
                generatorHit = hit;
                continue;
            }

            if (playerHit == null &&
                (hit.transform.root.CompareTag("Player") || hit.CompareTag("Player")))
            {
                playerHit = hit;
            }
        }

        // 호스트만 공격 판정 권한 보유 - 서버에 보고하면 서버가 S_*Damaged로 브로드캐스트
        if (!NetworkManager.Instance.IsHost) return;

        if (generatorHit != null)
        {
            NetworkManager.Instance.SendGame(GameProto.GamePacketId.CEnemyAttack, new GameProto.C_EnemyAttack
            {
                EnemyId        = _enemy.NetworkId,
                TargetPlayerId = 0,                                        // GENERATOR면 무시되는 필드
                Damage         = _enemy.Data.attackDamage,
                TargetType     = GameProto.AttackTargetType.AttackTargetGenerator,
            });
            return;
        }

        if (playerHit != null)
        {
            int targetPlayerId = GetHitPlayerId(playerHit.gameObject);
            NetworkManager.Instance.SendGame(GameProto.GamePacketId.CEnemyAttack, new GameProto.C_EnemyAttack
            {
                EnemyId        = _enemy.NetworkId,
                TargetPlayerId = targetPlayerId,
                Damage         = _enemy.Data.attackDamage,
                TargetType     = GameProto.AttackTargetType.AttackTargetPlayer,
            });
        }
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
