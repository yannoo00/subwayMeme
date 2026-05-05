using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCombat : MonoBehaviour
{
    [Header("Weapon Settings")]
    [SerializeField] private GameObject _fistPrefab;       // 기본 무기 (빈 슬롯 폴백)
    [SerializeField] private Transform  _weaponHolder;

    [Header("Drop Settings")]
    [SerializeField] private float _dropDistance = 1.5f;   // 슬롯 가득 시 드랍 거리(플레이어 정면)

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

        // HUD 초기 상태 동기화 - 두 슬롯 비어있고 0번 활성
        // Fist는 폴백 표시용이라 슬롯 데이터로는 null 전송 (HUD에서 Empty로 표시)
        PlayerEvents.WeaponSlotChanged(0, null);
        PlayerEvents.WeaponSlotChanged(1, null);
        PlayerEvents.ActiveSlotChanged(0, null);
    }

    private void Update()
    {
        if (GameManager.Instance.CurrentState == GameState.Menu) return;

        HandleSlotSwitch();

        // isPressed: 누르고 있는 동안 매 프레임 true. attackCooldown이 발사 간격을 제한
        // 클릭만 했다 떼면 한 프레임만 true → 1발, 누르고 있으면 쿨다운마다 연속 발사
        if (Mouse.current != null && Mouse.current.leftButton.isPressed)
            Attack();

        // R: 재장전 (원거리 무기에서만 의미 있음, 근접은 가상 메서드 비어있음)
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            ActiveWeapon.TryReload();
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

        // 슬롯 내용은 그대로, 활성만 바뀜 → ActiveSlotChanged 사용
        // 빈 슬롯이면 _slots[i]가 null이라 weaponData도 null로 전달됨 (Fist는 폴백이라 슬롯 데이터로 안 씀)
        PlayerEvents.ActiveSlotChanged(_activeSlotIndex, _slots[_activeSlotIndex]?.weaponData);
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

    // 픽업 성공 여부 반환. WeaponPickup은 true일 때만 자기 자신을 Destroy
    public bool TryPickupWeapon(GameObject weaponPrefab)
    {
        var newWeapon = weaponPrefab != null ? weaponPrefab.GetComponent<WeaponBase>() : null;
        if (newWeapon == null || newWeapon.weaponData == null) return false;

        // 같은 무기 중복 시 거부 (WeaponData 참조 비교)
        // Fist는 _slots에 없으므로 별도 검사 불필요
        for (int i = 0; i < _slots.Length; i++)
        {
            if (_slots[i] != null && _slots[i].weaponData == newWeapon.weaponData)
                return false;
        }

        // 빈 슬롯 우선 (0번부터)
        int targetSlot = -1;
        for (int i = 0; i < _slots.Length; i++)
        {
            if (_slots[i] == null) { targetSlot = i; break; }
        }

        // 슬롯 가득 → 활성 슬롯 무기 드랍 후 그 자리 차지
        if (targetSlot == -1)
        {
            DropWeapon(_activeSlotIndex);
            targetSlot = _activeSlotIndex;
        }

        EquipToSlot(targetSlot, weaponPrefab);
        return true;
    }


    // 지정 슬롯 무기를 바닥에 WeaponPickup으로 스폰. EquipToSlot이 이어서 기존 인스턴스를 Destroy
    private void DropWeapon(int slotIndex)
    {
        var current = _slots[slotIndex];
        if (current == null) return;

        var pickupPrefab = current.weaponData.pickupPrefab;
        if (pickupPrefab == null)
        {
            Debug.LogWarning($"[PlayerCombat] {current.weaponData.weaponName} pickupPrefab 미설정 - 스왑 시 무기가 회수 불가능 상태로 사라집니다.");
            return;
        }

        Vector3 dropPos = transform.position + transform.forward * _dropDistance;
        Instantiate(pickupPrefab, dropPos, Quaternion.identity);
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

        // 활성 슬롯에 새 무기가 들어왔으면 활성 무기 타입도 변경됨 → 탄약 패널 가시성 갱신용
        if (isActive)
            PlayerEvents.ActiveSlotChanged(slotIndex, weapon.weaponData);
    }
}
