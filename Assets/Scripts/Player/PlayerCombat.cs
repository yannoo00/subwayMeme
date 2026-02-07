using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCombat : MonoBehaviour
{
    

    [SerializeField] private WeaponBase _weapon;
    


    private void Update()
    {
        if(Mouse.current!=null && Mouse.current.leftButton.isPressed)
        {
            Attack();
        }
    }



    private void Attack()
    {
        _weapon.TryAttack();
    }


    public void EquipWeapon(WeaponBase newWeapon)
    {
        if(_weapon != null)
        {
            _weapon.Unequip();
        }

        _weapon = newWeapon;
        _weapon.Equip();
    }
}
