using UnityEngine;
using UnityEngine.InputSystem;


public class PlayerCombat : MonoBehaviour
{
    
    [Header ("Weapon Settings")]
    [SerializeField] private GameObject _defaultWeaponPrefab;
    [SerializeField] private Transform _weaponHolder;
    
    private WeaponBase _currentWeapon;
    private Animator _animator;


    private void Start()
    {
        _animator = GetComponentInChildren<Animator>();
        EquipWeapon(_defaultWeaponPrefab);
    }

    private void Update()
    {
        if (GameManager.Instance.CurrentState == GameState.Menu) return;
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Attack();
        }
    }

    private void Attack()
    {
        if (_currentWeapon.TryAttack())
            _animator?.SetTrigger(AnimatorParams.Attack);
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
