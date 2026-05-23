using System.Collections;
using UnityEngine;

// 원격 플레이어 오브젝트에 붙는 컴포넌트
// S_Move 수신 시 SetTargetState()로 목표 상태를 갱신하고
// Update()에서 Lerp/Slerp로 부드럽게 보간
public class NetworkPlayer : MonoBehaviour
{
    public int    PlayerId   { get; private set; }
    public string PlayerName { get; private set; }
    public bool IsAlive = true;

    private Animator  _animator;
    private Transform _modelTransform;
    private Transform _weaponSocket;            // 무기 부착 지점 - Binder 가 모델에서 찾아준 값

    private Vector3 _targetPos;
    private float   _targetRotY;
    private float   _targetSpeed;
    private float   _targetStrafeX;
    private float   _targetStrafeY;

    // PlayerAnimator 와 동일한 damping 값 - 로컬/원격 애니메이션 톤 통일
    private const float MOTION_DAMP_TIME = 0.1f;

    // 현재 부착된 무기 인스턴스. Aim true 에 스폰, false 에 디스폰 - WeaponSocket 에만 존재
    private GameObject _currentWeaponInstance;
    // duration 기반 bool 코루틴 (Reload / WeaponSwitch) - 중복 시작 시 기존 것 취소
    private Coroutine _reloadCoroutine;
    private Coroutine _switchCoroutine;

    // === 초기화 ===

    // PlayerRegistry.SpawnRemotePlayer 가 Instantiate 직후 호출
    // characterId 는 GamePlayerInfo.CharacterId (다른 사람이 선택한 캐릭터)
    public void Init(int playerId, string playerName, int characterId)
    {
        PlayerId   = playerId;
        PlayerName = playerName;
        gameObject.name = $"RemotePlayer_{playerId}_{playerName}";

        // 모델 동적 바인딩 - RemotePlayer 프리팹의 Binder 는 _autoBindOnAwake=false 로 두어야 함
        // (여기서 명시 호출하지 않으면 ModelHolder 가 비어있는 상태로 남음)
        if (TryGetComponent<PlayerCharacterBinder>(out var binder))
        {
            binder.Bind(characterId);
            _weaponSocket = binder.WeaponSocket;
        }
        else
        {
            Debug.LogWarning($"[NetworkPlayer] PlayerCharacterBinder 미부착 - 모델 바인딩 스킵 (id={playerId})");
        }

        // Bind 직후에 Animator 취득 - Binder가 ModelHolder 자식으로 새 모델을 넣었으므로 그 안의 Animator를 잡음
        _animator       = GetComponentInChildren<Animator>();
        // 회전 대상은 ModelHolder - 그 자식인 동적 모델이 함께 회전함 (LocalPlayer 프리팹 구조와 일치)
        _modelTransform = transform.Find("ModelHolder") ?? transform;
        _targetPos      = transform.position;
    }

    // === S_Move 수신 시 호출 ===
    // 목표값만 저장 - 실제 보간은 Update()에서 매 프레임 수행
    // (패킷 수신은 50ms 주기지만 SetFloat damping 은 deltaTime 기반이라 매 프레임 호출이 자연스러움)
    public void SetTargetState(Vector3 pos, float rotY, float speed, float strafeX, float strafeY)
    {
        _targetPos     = pos;
        _targetRotY    = rotY;
        _targetSpeed   = speed;
        _targetStrafeX = strafeX;
        _targetStrafeY = strafeY;
    }

    // S_Attack 수신 시 호출 - 다른 플레이어의 공격 애니메이션 재생
    public void TriggerAttackAnim()
    {
        _animator?.SetTrigger(AnimatorParams.Attack);
    }


    // === 무기 액션 (S_Aiming / S_Reload / S_WeaponSwitch) ===

    // S_Aiming 수신 시 호출.
    // isAiming=true 면 WeaponDatabase 에서 weaponType 으로 프리팹을 가져와 _weaponSocket 자식으로 스폰.
    // isAiming=false 면 기존 인스턴스 destroy.
    // 둘 다 Animator IsAiming bool + WeaponTypeID 정수를 같이 갱신해 로컬과 동일 톤의 애니메이션이 재생되도록.
    public void OnAiming(bool isAiming, int weaponTypeId)
    {
        _animator?.SetBool(AnimatorParams.IsAiming, isAiming);
        _animator?.SetInteger(AnimatorParams.WeaponTypeID, weaponTypeId);

        if (isAiming)
            SpawnWeapon((WeaponType)weaponTypeId);
        else
            DespawnWeapon();
    }

    // S_Reload 수신 시 호출. duration 동안 IsReloading=true 유지 후 자동 false
    // (PlayerAnimator.HandleReloadStarted 와 동일한 SetBoolForDuration 패턴)
    public void OnReload(float duration)
    {
        if (_animator == null) return;
        if (_reloadCoroutine != null) StopCoroutine(_reloadCoroutine);
        _reloadCoroutine = StartCoroutine(SetBoolForDuration(AnimatorParams.IsReloading, duration));
    }

    // S_WeaponSwitch 수신 시 호출. duration 동안 IsSwitchingWeapon=true 유지 후 false,
    // 종료 시점에 WeaponTypeID 를 newWeaponTypeId 로 갱신해 새 무기 애니메이션으로 전환
    public void OnWeaponSwitch(float duration, int newWeaponTypeId)
    {
        if (_animator == null) return;
        if (_switchCoroutine != null) StopCoroutine(_switchCoroutine);
        _switchCoroutine = StartCoroutine(WeaponSwitchCoroutine(duration, newWeaponTypeId));
    }


    // === 무기 헬퍼 ===

    private void SpawnWeapon(WeaponType type)
    {
        DespawnWeapon();   // 혹시 남아있던 인스턴스 정리

        if (_weaponSocket == null)
        {
            Debug.LogWarning($"[NetworkPlayer] WeaponSocket 미설정 - 무기 스폰 스킵 (id={PlayerId})");
            return;
        }

        var db = WeaponDatabase.Instance;
        if (db == null)
        {
            Debug.LogWarning("[NetworkPlayer] WeaponDatabase 싱글톤 없음 - Persistent 씬에 배치되었는지 확인");
            return;
        }

        var prefab = db.GetPrefab(type);
        if (prefab == null)
        {
            Debug.LogWarning($"[NetworkPlayer] WeaponType={type} 에 매핑된 프리팹이 WeaponDatabase 에 없음");
            return;
        }

        // 프리팹의 WeaponBase.Equip() 은 호출하지 않음 - HitscanWeapon.Equip 이 AmmoChanged 같은 로컬 HUD
        // 이벤트를 발행하므로 원격 인스턴스에서는 부작용. Instantiate 만으로 SetActive(true) 상태가 되고,
        // 메쉬는 SetVisible(true) 로 명시 표시.
        _currentWeaponInstance = Instantiate(prefab, _weaponSocket);
        _currentWeaponInstance.transform.localPosition = Vector3.zero;
        _currentWeaponInstance.transform.localRotation = Quaternion.identity;

        if (_currentWeaponInstance.TryGetComponent<WeaponBase>(out var wb))
            wb.SetVisible(true);
    }

    private void DespawnWeapon()
    {
        if (_currentWeaponInstance != null)
        {
            Destroy(_currentWeaponInstance);
            _currentWeaponInstance = null;
        }
    }

    private IEnumerator SetBoolForDuration(int paramHash, float duration)
    {
        _animator.SetBool(paramHash, true);
        yield return new WaitForSeconds(duration);
        _animator.SetBool(paramHash, false);
    }

    private IEnumerator WeaponSwitchCoroutine(float duration, int newWeaponTypeId)
    {
        _animator.SetBool(AnimatorParams.IsSwitchingWeapon, true);
        yield return new WaitForSeconds(duration);
        _animator.SetBool(AnimatorParams.IsSwitchingWeapon, false);
        // 스왑 완료 시점에 새 weaponType 으로 갱신 - 이후 Aim 시 새 무기 포즈가 적용됨
        _animator.SetInteger(AnimatorParams.WeaponTypeID, newWeaponTypeId);
    }


    // === 보간 ===

    private void Update()
    {
        // 위치: 50ms 주기 패킷 사이에 자연스럽게 도달하는 속도
        transform.position = Vector3.Lerp(transform.position, _targetPos, 30f * Time.deltaTime);

        // 회전: PlayerModel만 회전 (PlayerRoot는 카메라와 공유하지 않으므로 여기선 동일)
        Quaternion targetRot = Quaternion.Euler(0f, _targetRotY, 0f);
        _modelTransform.rotation = Quaternion.Slerp(_modelTransform.rotation, targetRot, 30f * Time.deltaTime);

        // 애니메이션 Blend Tree 입력 - PlayerAnimator 와 동일한 SetFloat damping 사용
        if (_animator != null)
        {
            _animator.SetFloat(AnimatorParams.Speed,   _targetSpeed,   MOTION_DAMP_TIME, Time.deltaTime);
            _animator.SetFloat(AnimatorParams.StrafeX, _targetStrafeX, MOTION_DAMP_TIME, Time.deltaTime);
            _animator.SetFloat(AnimatorParams.StrafeY, _targetStrafeY, MOTION_DAMP_TIME, Time.deltaTime);
        }
    }
}
