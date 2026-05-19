using UnityEngine;

public abstract class WeaponBase : MonoBehaviour
{
    [Header("Weapon Data")]
    [SerializeField] protected WeaponData _weaponData;


    protected float _lastAttackTime;

    public WeaponData weaponData => _weaponData;
    public bool CanAttack => Time.time >= _lastAttackTime + _weaponData.attackCooldown;


    public bool TryAttack()
    {
        if (!CanAttack) return false;

        _lastAttackTime = Time.time;
        PerformAttack();
        return true;
    }


    public virtual void Equip()
    {
        gameObject.SetActive(true);
    }

    public virtual void Unequip()
    {
        gameObject.SetActive(false);
    }

    // 재장전이 필요한 무기에서 오버라이드 (HitscanWeapon 등). 기본은 no-op
    public virtual void TryReload() { }

    protected abstract void PerformAttack();
}
 