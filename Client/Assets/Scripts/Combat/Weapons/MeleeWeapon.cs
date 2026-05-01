using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class MeleeWeapon : WeaponBase
{
    [Header("Attack Settings")]
    [SerializeField] private Transform _attackPoint;
    [SerializeField] private LayerMask _targetLayer;


    [Header("Debug")]
    [SerializeField] private bool _showDebugGizmo = true;


    protected override void PerformAttack()
    {
        Vector3 attackPosition = _attackPoint != null
            ? _attackPoint.position
            : transform.position;

        Collider[] hits = Physics.OverlapSphere(attackPosition, _weaponData.range, _targetLayer);
        
        var hitEnemyIds = new List<int>();

        foreach (Collider hit in hits)
        {
            if (!IsInAttackAngle(hit.transform)) continue;

            var enemy = hit.GetComponentInParent<Enemy>();
            bool isNetworkEnemy = enemy != null && enemy.NetworkId > 0;

            // 네트워크 적은 서버가 HP 계산 - 로컬 TakeDamage 호출 안 함
            if (!isNetworkEnemy && hit.TryGetComponent<IDamageable>(out var damageable))
                damageable.TakeDamage(_weaponData.damage);

            if (isNetworkEnemy)
                hitEnemyIds.Add(enemy.NetworkId);

            if (!_weaponData.isSplash) break;
        }

        if (hitEnemyIds.Count > 0)
        {
            var pkt = new GameProto.C_Attack { WeaponId = 0, Damage = _weaponData.damage };
            pkt.HitEnemyIds.AddRange(hitEnemyIds);
            NetworkManager.Instance.SendGame(GameProto.GamePacketId.CAttack, pkt);
        }
    }


    private bool IsInAttackAngle(Transform target)
    {
        Vector3 directionToTarget = (target.position - transform.position).normalized;
        float angle = Vector3.Angle(transform.forward, directionToTarget);
        return angle <= _weaponData.attackAngle / 2f;
    }




    //Debug
    private void OnDrawGizmosSelected()
    {
        if(!_showDebugGizmo || _weaponData == null) return;

        Vector3 attackPosition = _attackPoint != null
            ? _attackPoint.position
            : transform.position;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPosition, _weaponData.range);
    }
}
