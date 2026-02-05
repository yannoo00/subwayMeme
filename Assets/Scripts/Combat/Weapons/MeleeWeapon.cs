using System;
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
        
        foreach (Collider hit in hits)
        {
            if (!IsInAttackAngle(hit.transform)) continue; 

            if (hit.TryGetComponent<IDamageable>(out var damageable))
            {
                damageable.TakeDamage(_weaponData.damage);
            }
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
