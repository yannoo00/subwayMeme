using UnityEngine;

public abstract class WeaponBase : MonoBehaviour
{
    [Header("Weapon Data")]
    [SerializeField] protected WeaponData _weaponData;

    // 무기 메쉬가 들어있는 자식 오브젝트. 조준 토글로 가시성만 제어할 때 사용.
    // SetActive로 메쉬 루트만 끄는 이유: 부모(this) Update는 살려서 재장전 타이머가 계속 돌게 함
    [Header("Model")]
    [SerializeField] private GameObject _model;


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

    // 조준 토글용 가시성 제어. 모델 자식만 끄므로 부모 Update/타이머는 계속 진행됨
    public virtual void SetVisible(bool visible)
    {
        if (_model != null) _model.SetActive(visible);
    }

    // 재장전이 필요한 무기에서 오버라이드 (HitscanWeapon 등). 기본은 no-op
    public virtual void TryReload() { }

    protected abstract void PerformAttack();
}
 