using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCombat : MonoBehaviour
{
    // 캐릭터 모델의 WeaponSocket Transform. PlayerCharacterBinder가 Awake에서 주입.
    private Transform  _weaponHolder;

    [Header("Drop Settings")]
    [SerializeField] private float _dropDistance = 1.5f;   // 슬롯 가득 시 드랍 거리(플레이어 정면)

    [Header("Switching")]
    // 무기 스위칭 애니메이션 길이 - 이 시간 동안 IsSwitchingWeapon=true 유지하고
    // 시간이 끝나는 시점에 실제 무기 swap 수행 (예제 ShooterAnimator와 동일한 패턴)
    [SerializeField] private float _weaponSwitchDuration = 0.4f;

    // 무기 슬롯: 0 = 1번키, 1 = 2번키
    private WeaponBase[] _slots     = new WeaponBase[2];
    private int          _activeSlotIndex = 0;

    // 스킬 슬롯: 0 = 3번키, 1 = 4번키
    private SkillBase[] _skillSlots = new SkillBase[2];

    private bool _isAiming;
    private bool _isSwitching;            // 스위칭 코루틴 진행 중 - 발사/중복 스위치 차단용
    private Coroutine _switchCoroutine;
    private PlayerStats _stats;

    // 활성 슬롯에 무기가 없으면 null - 호출 측에서 null 체크 필요
    private WeaponBase ActiveWeapon => _slots[_activeSlotIndex];


    // === 외부 주입 ===

    // PlayerCharacterBinder가 Awake에서 호출. 픽업으로 들어오는 무기 프리팹의
    // Instantiate 부모로 사용되므로 EquipToSlot 호출 전에는 반드시 주입되어야 함
    public void SetWeaponHolder(Transform holder)
    {
        _weaponHolder = holder;
    }


    // === Unity 생명주기 ===

    private void Start()
    {
        _stats    = GetComponent<PlayerStats>();

        // HUD 초기 상태 동기화 - 두 슬롯 비어있고 0번 활성
        PlayerEvents.WeaponSlotChanged(0, null);
        PlayerEvents.WeaponSlotChanged(1, null);
        PlayerEvents.ActiveSlotChanged(0, null);
    }

    private void Update()
    {
        if (GameManager.Instance.CurrentState == GameState.Menu) return;
        // 사망 시 입력 차단 - 관전 모드로 전환되므로 전투 입력은 무시
        if (_stats != null && !_stats.IsAlive) return;

        HandleSlotSwitch();

        // isPressed: 누르고 있는 동안 매 프레임 true. attackCooldown이 발사 간격을 제한
        // 클릭만 했다 떼면 한 프레임만 true → 1발, 누르고 있으면 쿨다운마다 연속 발사
        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
            HandleAimToggle();

        if (Mouse.current != null && Mouse.current.leftButton.isPressed)
            Attack();

        // R: 재장전 (빈 슬롯이면 ActiveWeapon == null)
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            ActiveWeapon?.TryReload();
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
        if (_isSwitching) return;   // 코루틴 진행 중에는 중복 입력 무시 (예제와 동일)

        // 비조준 상태에서는 스왑 대기 시간 없이 즉시 스왑
        if (!_isAiming)
        {
            PerformSwap(slotIndex);
            return;
        }

        _switchCoroutine = StartCoroutine(SwitchToSlotCoroutine(slotIndex));
    }

    // 스위칭 애니메이션 길이만큼 대기 후 실제 무기 swap.
    // IsSwitchingWeapon bool 동기화는 PlayerAnimator가 OnWeaponSwitchStarted 구독으로 처리.
    private IEnumerator SwitchToSlotCoroutine(int slotIndex)
    {
        _isSwitching = true;
        PlayerEvents.WeaponSwitchStarted(_weaponSwitchDuration);

        yield return new WaitForSeconds(_weaponSwitchDuration);

        PerformSwap(slotIndex);

        _isSwitching = false;
        _switchCoroutine = null;
    }

    // 실제 무기 교체 동작. 즉시 경로와 코루틴 경로가 공유
    private void PerformSwap(int slotIndex)
    {
        ActiveWeapon?.Unequip();
        _activeSlotIndex = slotIndex;
        ActiveWeapon?.Equip();

        // 빈 슬롯으로 전환 시 조준 해제 - 들고 있는 무기가 없으면 조준 상태 유지 의미 없음
        if (_isAiming && ActiveWeapon == null)
            SetAiming(false);

        // 슬롯 내용은 그대로, 활성만 바뀜 → ActiveSlotChanged 사용
        // 빈 슬롯이면 _slots[i]가 null이라 weaponData도 null로 전달됨
        PlayerEvents.ActiveSlotChanged(_activeSlotIndex, _slots[_activeSlotIndex]?.weaponData);
    }


    // === 조준 ===

    private void HandleAimToggle()
    {
        // 무기가 없으면 조준 불가
        if (ActiveWeapon == null) return;
        SetAiming(!_isAiming);
    }

    private void SetAiming(bool isAiming)
    {
        if (_isAiming == isAiming) return;
        _isAiming = isAiming;
        PlayerEvents.AimStateChanged(_isAiming);
    }
    public bool IsAiming() => _isAiming;




    #region Attack
    // === 전투 ===
    private void Attack()
    {
        // 무기 스위칭 도중 발사 차단 (예제 WeaponController.HandleFirePressed와 동일한 가드)
        // 재장전 중 차단은 무기 측 TryAttack에서 처리
        if (_isSwitching) return;
        if (ActiveWeapon == null) return;

        if (ActiveWeapon.TryAttack())
            PlayerEvents.AttackPerformed();
    }

    #endregion


    #region Skills
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

    #endregion

    #region weapon
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

        // 슬롯이 가득 찼을 경우 현재 활성 슬롯 무기 드랍 후 그 자리 차지
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
        //현재 장착하려는 슬롯이 활성 슬롯인지? 
        bool isActive = slotIndex == _activeSlotIndex;

        // 기존 슬롯 무기 제거
        if (_slots[slotIndex] != null)
        {
            if (isActive) _slots[slotIndex].Unequip();
            Destroy(_slots[slotIndex].gameObject);
        }

        var weaponObj = Instantiate(prefab, _weaponHolder);
        var weapon    = weaponObj.GetComponent<WeaponBase>();
        _slots[slotIndex] = weapon;

        if (isActive)
            weapon.Equip();
        else
            weapon.Unequip();

        PlayerEvents.WeaponSlotChanged(slotIndex, weapon.weaponData);

        // 활성 슬롯에 새 무기가 들어왔으면 활성 무기 타입도 변경됨. 탄약 패널 갱신
        if (isActive)
            PlayerEvents.ActiveSlotChanged(slotIndex, weapon.weaponData);
    }

    #endregion
}
