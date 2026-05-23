using System.Collections;
using UnityEngine;

// 로컬 플레이어 애니메이션 전담 컴포넌트
// PlayerController/PlayerCombat/PlayerStats 는 Animator 를 모르고 상태/이벤트만 노출/발행한다
//
// 두 가지 채널 혼용:
//  - 연속값(IsMoving 등): 매 프레임 PlayerController 의 프로퍼티를 폴링하여 SetBool/SetFloat
//  - 단발 액션(Attack/Death): PlayerEvents 구독으로 SetTrigger/SetBool
//  - 지속 상태(IsReloading/IsSwitchingWeapon): 시작 이벤트의 duration 을 받아
//    코루틴으로 대기 후 SetBool(false) - Animation Event 의존 없이 동기화
//
// 모델은 PlayerCharacterBinder 가 Awake 에서 동적으로 인스턴시에이션하므로
// Animator 탐색은 Start 시점에 GetComponentInChildren 으로 수행
[RequireComponent(typeof(PlayerController))]
public class PlayerAnimator : MonoBehaviour
{
    private Animator _animator;
    private PlayerController _controller;

    // 진행 중인 bool 클리어 코루틴 - 중복 시작 시 기존 것 취소하고 새로 시작
    private Coroutine _reloadCoroutine;
    private Coroutine _switchCoroutine;

    // 조준 상태 캐싱 - Strafe 폴링이 매 프레임 PlayerCombat 메서드 호출 없이 처리되도록
    private bool _isAiming;

    // SetFloat 보간 시간 - 작을수록 즉각, 클수록 부드러움. 예제도 0.1 사용
    private const float MOTION_DAMP_TIME = 0.1f;


    private void Awake()
    {
        _controller = GetComponent<PlayerController>();
    }

    private void Start()
    {
        _animator = GetComponentInChildren<Animator>();

        if (_animator == null)
            Debug.LogWarning($"[PlayerAnimator] {gameObject.name} 자식에서 Animator를 찾지 못했습니다.");

        _animator.SetFloat("MotionSpeed", 1f);
    }


    private void OnEnable()
    {
        PlayerEvents.OnAttackPerformed      += HandleAttack;
        PlayerEvents.OnPlayerDied           += HandleDeath;
        PlayerEvents.OnReloadStarted        += HandleReloadStarted;
        PlayerEvents.OnWeaponSwitchStarted  += HandleWeaponSwitchStarted;
        PlayerEvents.OnAimStateChanged      += HandleAimStateChanged;
    }

    private void OnDisable()
    {
        PlayerEvents.OnAttackPerformed      -= HandleAttack;
        PlayerEvents.OnPlayerDied           -= HandleDeath;
        PlayerEvents.OnReloadStarted        -= HandleReloadStarted;
        PlayerEvents.OnWeaponSwitchStarted  -= HandleWeaponSwitchStarted;
        PlayerEvents.OnAimStateChanged      -= HandleAimStateChanged;
    }


    // === 폴링: 연속 상태 ===

    private void Update()
    {
        if (_animator == null) return;

        // Speed Blend Tree (0=idle, 1=walk, dashMultiplier=dash)
        _animator.SetFloat(AnimatorParams.Speed, _controller.NormalizedSpeed, MOTION_DAMP_TIME, Time.deltaTime);

        // 조준 중에만 스트레이프 입력 전달, 비조준 시에는 0으로 수렴
        // (비조준 시에는 몸이 이동 방향으로 회전하므로 항상 forward 모션이라 strafe 무의미)
        Vector2 strafe = _isAiming ? _controller.MoveInput : Vector2.zero;
        _animator.SetFloat(AnimatorParams.StrafeX, strafe.x, MOTION_DAMP_TIME, Time.deltaTime);
        _animator.SetFloat(AnimatorParams.StrafeY, strafe.y, MOTION_DAMP_TIME, Time.deltaTime);
    }


    // === 이벤트 핸들러: 단발 액션 ===

    private void HandleAttack()
    {
        _animator?.SetTrigger(AnimatorParams.Attack);
    }
    private void HandleDeath()  => _animator?.SetBool(AnimatorParams.IsDead, true);

    private void HandleAimStateChanged(bool isAiming, int weaponTypeID)
    {
        _isAiming = isAiming;   // Update의 Strafe 폴링에서 참조
        _animator?.SetBool(AnimatorParams.IsAiming, isAiming);
        _animator?.SetInteger(AnimatorParams.WeaponTypeID, weaponTypeID);
    }


    // === 이벤트 핸들러: 지속 상태 (duration 기반 자동 종료) ===
    private void HandleReloadStarted(float duration)
    {
        if (_animator == null) return;
        if (_reloadCoroutine != null) StopCoroutine(_reloadCoroutine);
        _reloadCoroutine = StartCoroutine(SetBoolForDuration(AnimatorParams.IsReloading, duration));
    }

    private void HandleWeaponSwitchStarted(float duration)
    {
        if (_animator == null) return;
        if (_switchCoroutine != null) StopCoroutine(_switchCoroutine);
        _switchCoroutine = StartCoroutine(SetBoolForDuration(AnimatorParams.IsSwitchingWeapon, duration));
    }

    // bool 을 true 로 올리고 duration 후 false 로 내림.
    // 도중에 같은 이벤트가 다시 들어오면 호출 측에서 StopCoroutine 으로 취소하므로
    // 여기서는 단순 대기 후 클리어만 책임진다.
    private IEnumerator SetBoolForDuration(int paramHash, float duration)
    {
        _animator.SetBool(paramHash, true);
        yield return new WaitForSeconds(duration);
        _animator.SetBool(paramHash, false);
    }
}
