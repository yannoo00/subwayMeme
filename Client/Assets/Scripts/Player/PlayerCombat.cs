using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCombat : MonoBehaviour
{
    [Header("Weapon Settings")]
    [SerializeField] private GameObject _fistPrefab;       // 기본 무기 (빈 슬롯 폴백)
    [SerializeField] private Transform  _weaponHolder;

    // 무기 슬롯: 0 = 1번키, 1 = 2번키
    private WeaponBase[] _slots     = new WeaponBase[2];
    private WeaponBase   _fistInstance;
    private int          _activeSlotIndex = 0;

    // 스킬 슬롯: 0 = 3번키, 1 = 4번키
    private SkillBase[] _skillSlots = new SkillBase[2];

    private Animator _animator;

    // 활성 슬롯에 무기가 없으면 Fist 반환
    private WeaponBase ActiveWeapon => _slots[_activeSlotIndex] ?? _fistInstance;


    // === Unity 생명주기 ===

    private void Start()
    {
        _animator = GetComponentInChildren<Animator>();

        var fistObj  = Instantiate(_fistPrefab, _weaponHolder);
        _fistInstance = fistObj.GetComponent<WeaponBase>();
        _fistInstance.Equip();
    }

    private void Update()
    {
        if (GameManager.Instance.CurrentState == GameState.Menu) return;

        HandleSlotSwitch();

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            Attack();
    }


    // === 슬롯 전환 ===

    private void HandleSlotSwitch()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.digit1Key.wasPressedThisFrame) SwitchToSlot(0);
        if (Keyboard.current.digit2Key.wasPressedThisFrame) SwitchToSlot(1);
        if (Keyboard.current.digit3Key.wasPressedThisFrame) ActivateSkill(0);
        if (Keyboard.current.digit4Key.wasPressedThisFrame) ActivateSkill(1);
    }

    private void SwitchToSlot(int slotIndex)
    {
        if (slotIndex == _activeSlotIndex) return;

        ActiveWeapon.Unequip();
        _activeSlotIndex = slotIndex;
        ActiveWeapon.Equip();

        PlayerEvents.WeaponSlotChanged(_activeSlotIndex, ActiveWeapon.weaponData);
    }


    // === 전투 ===

    private void Attack()
    {
        if (ActiveWeapon.TryAttack())
            _animator?.SetTrigger(AnimatorParams.Attack);
    }


    // === 스킬 발동 ===

    private void ActivateSkill(int skillSlotIndex)
    {
        _skillSlots[skillSlotIndex]?.TryActivate();
    }

    // 변이 시스템에서 호출 - 스킬을 슬롯에 배정
    public void AssignSkill(int skillSlotIndex, GameObject skillPrefab)
    {
        if (skillSlotIndex < 0 || skillSlotIndex >= _skillSlots.Length) return;

        if (_skillSlots[skillSlotIndex] != null)
            Destroy(_skillSlots[skillSlotIndex].gameObject);

        var skillObj = Instantiate(skillPrefab, transform);
        _skillSlots[skillSlotIndex] = skillObj.GetComponent<SkillBase>();

        PlayerEvents.SkillSlotChanged(skillSlotIndex, _skillSlots[skillSlotIndex].SkillData);
    }


    // === 무기 획득 (WeaponPickup에서 호출) ===

    public void TryPickupWeapon(GameObject weaponPrefab)
    {
        // 빈 슬롯 우선 배정 (0번 먼저)
        int targetSlot = -1;
        for (int i = 0; i < _slots.Length; i++)
        {
            if (_slots[i] == null) { targetSlot = i; break; }
        }

        // 빈 슬롯 없으면 현재 활성 슬롯 교체
        if (targetSlot == -1)
            targetSlot = _activeSlotIndex;

        EquipToSlot(targetSlot, weaponPrefab);
    }

    private void EquipToSlot(int slotIndex, GameObject prefab)
    {
        bool isActive = slotIndex == _activeSlotIndex;

        // 기존 슬롯 무기 제거
        if (_slots[slotIndex] != null)
        {
            if (isActive) _slots[slotIndex].Unequip();
            Destroy(_slots[slotIndex].gameObject);
        }
        else if (isActive)
        {
            // 슬롯이 비어있었으면 Fist가 표시 중 → 숨기기
            _fistInstance.Unequip();
        }

        var weaponObj = Instantiate(prefab, _weaponHolder);
        var weapon    = weaponObj.GetComponent<WeaponBase>();
        _slots[slotIndex] = weapon;

        if (isActive)
            weapon.Equip();
        else
            weapon.Unequip();

        PlayerEvents.WeaponSlotChanged(slotIndex, weapon.weaponData);
    }
}
