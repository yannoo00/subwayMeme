using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCombat : MonoBehaviour
{
    
    [Header ("Weapon Settings")]
    [SerializeField] private GameObject _defaultWeaponPrefab;
    [SerializeField] private Transform _weaponHolder;
    
    private WeaponBase _currentWeapon;

    
    private void Start()
    {
        EquipWeapon(_defaultWeaponPrefab);
    }

    private void Update()
    {
        if(Mouse.current!=null && Mouse.current.leftButton.isPressed)
        {
            //Debug.Log("try attack");
            Attack();
        }
    }
    private void Attack()
    {
        _currentWeapon.TryAttack();
    }




    public void EquipWeapon(GameObject weaponPrefab)
    {
        if(_currentWeapon != null)
        {
            _currentWeapon.Unequip();
        }

        GameObject weaponObject = Instantiate(weaponPrefab, _weaponHolder);
        _currentWeapon = weaponObject.GetComponent<WeaponBase>();
        _currentWeapon.Equip();
    }
}
