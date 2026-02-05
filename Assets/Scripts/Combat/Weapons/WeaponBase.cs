using Unity.VisualScripting;
using UnityEngine;

public abstract class WeaponBase : MonoBehaviour
{
    [Header("Weapon Data")]
    [SerializeField] protected WeaponData _weaponData;


    protected float _lastAttackTime;

    public WeaponData weaponData => _weaponData;
    public bool CanAttack => Time.time >= _lastAttackTime + _weaponData.attackCooldown;


    public void TryAttack()
    {
        if (!CanAttack) return;

        _lastAttackTime = Time.time;
        PerformAttack();
    }


    public virtual void Equip()
    {
        gameObject.SetActive(true);
    }

    public virtual void Unequip()
    {
        gameObject.SetActive(false);
    }

    protected abstract void PerformAttack();
}
