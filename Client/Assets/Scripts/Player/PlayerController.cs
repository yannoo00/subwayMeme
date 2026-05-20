using System.Collections;
using GameProto;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    // CharacterDefinition.baseMoveSpeed가 없을 때 fallback
    [SerializeField] private float _moveSpeed = 5f;
    // Shift 누른 상태로 이동 시 _moveSpeed에 곱해지는 배율
    [SerializeField] private float _dashMultiplier = 2f;
    // 카메라 forward를 기준으로 이동 방향 계산
    // PlayerRegistry가 동적 스폰하므로 Inspector 참조는 보통 끊김 -> Camera.main으로 자동 탐색
    private Transform _cameraTransform;
    // 회전 대상: PlayerRoot 대신 PlayerModel만 회전시켜 카메라가 같이 돌지 않도록
    [SerializeField] private Transform _playerModel;
    // 캐릭터가 이동 방향으로 회전하는 속도
    [SerializeField] private float _rotationSpeed = 15f;

    [Header("Dodge")]
    [SerializeField] private float _dodgeDistance = 4f;
    [SerializeField] private float _dodgeDuration = 0.25f;
    // CharacterDefinition.baseDodgeCooldown이 없을 때 fallback
    [SerializeField] private float _dodgeCooldown = 1.5f;

    private PlayerStats _stats;
    private bool _isDodging = false;
    private bool _isAiming = false;
    private float _lastDodgeTime = -Mathf.Infinity;

    // PlayerAnimator가 매 프레임 폴링하여 Animator의 Speed Blend Tree 입력으로 사용
    // 컨트롤러 자체는 Animator를 모름 - 상태만 노출
    // 0 = 정지, 1 = 일반 이동, _dashMultiplier (기본 2) = dash 이동
    public float NormalizedSpeed { get; private set; }
    // 조준 중 StrafeX/Y Blend Tree 입력. 키 입력 기반 -1~1
    public Vector2 MoveInput { get; private set; }
    // Shift 누르고 이동 중인 상태. 정지 상태에서 Shift만 누르면 false
    public bool IsDashing { get; private set; }

    // 이동 동기화: 20Hz 고정 주기 + Dead Zone 필터
    private const float SEND_INTERVAL  = 0.05f;   // 20Hz
    private const float POS_THRESHOLD  = 0.01f;   // 위치 변화 임계값 (m)
    private const float ROT_THRESHOLD  = 1f;       // 회전 변화 임계값 (도)
    private float   _lastSendTime;
    private Vector3 _lastSentPos;
    private float   _lastSentRotY;
    private bool    _lastSentIsMoving;
    private bool    _lastSentIsDodging;


    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        _stats = GetComponent<PlayerStats>();

        TryAcquireCameraTransform();

        // 캐릭터 베이스 스탯 적용 - SelectedCharacter가 없으면 Inspector fallback 유지
        var charDef = GameManager.Instance?.SelectedCharacter;
        if (charDef != null)
        {
            _moveSpeed     = charDef.baseMoveSpeed;
            _dodgeCooldown = charDef.baseDodgeCooldown;
        }
    }

    // Camera.main 은 "MainCamera" 태그가 붙은 첫 번째 활성 카메라를 반환
    // Unity 2020.2+ 부터 내부 캐싱이라 매 프레임 호출해도 부담 적음
    private void TryAcquireCameraTransform()
    {
        if (_cameraTransform != null) return;
        
        var cam = Camera.main;

        Debug.Log("[PlayerController] Camera.main 탐색: " + (cam != null ? cam.name : "null"));

        if (cam != null) _cameraTransform = cam.transform;
    }

    private void OnEnable()
    {
        PlayerEvents.OnAimStateChanged += OnAimStateChanged;
    }

    private void OnDisable()
    {
        PlayerEvents.OnAimStateChanged -= OnAimStateChanged;
    }

    private void OnAimStateChanged(bool isAiming, int weaponTypeID)
    {
        _isAiming = isAiming;
    }


    void Update()
    {
        // early return 경로에서도 폴링값이 stale하게 남지 않도록 매 프레임 기본값
        // (사망/닷지/메뉴 등에서 walk 애니메이션이 끊기지 않는 버그 방지)
        NormalizedSpeed = 0f;
        MoveInput       = Vector2.zero;
        IsDashing       = false;

        if (GameManager.Instance.CurrentState != GameState.Playing) return;
        if (Keyboard.current == null) return;
        if (_isDodging) return;
        if (!_stats.IsAlive) return;

        // Awake 시점에 카메라가 없을 수도 있어 매 프레임 폴백 (이미 잡혔으면 즉시 return)
        TryAcquireCameraTransform();

        Vector2 input = new Vector2(
            (Keyboard.current.dKey.isPressed ? 1 : 0) - (Keyboard.current.aKey.isPressed ? 1 : 0),
            (Keyboard.current.wKey.isPressed ? 1 : 0) - (Keyboard.current.sKey.isPressed ? 1 : 0));

        bool isMoving = input != Vector2.zero;
        // Shift 단독으로는 dash 아님 - 이동 중일 때만 의미
        // 조준 중에는 Shift 눌러도 dash 금지 - 스트레이프 정밀도 유지 + 조준=신중한 무빙 컨셉
        IsDashing = isMoving && Keyboard.current.leftShiftKey.isPressed && !_isAiming;
        MoveInput = input;
        // dash면 Blend Tree에서 dash 모션 (예: threshold 2)으로 가도록 _dashMultiplier 그대로 사용
        NormalizedSpeed = isMoving ? (IsDashing ? _dashMultiplier : 1f) : 0f;

        // 스페이스바 닷지 입력 (쿨타임 체크 포함)
        if (Keyboard.current.spaceKey.wasPressedThisFrame && CanDodge())
        {
            StartCoroutine(DodgeCoroutine());
            return;
        }

        Vector3 camForward = _cameraTransform != null ? _cameraTransform.forward : transform.forward;
        camForward.y = 0f;
        camForward.Normalize();
        Vector3 camRight = _cameraTransform != null ? _cameraTransform.right : transform.right;
        camRight.y = 0f;
        camRight.Normalize();

        Transform rotateTarget = _playerModel != null ? _playerModel : transform;

        if (input == Vector2.zero)
        {
            // 조준 중에는 정지 상태에서도 카메라 방향으로 몸체를 회전
            if (_isAiming)
            {
                Quaternion aimRot = Quaternion.LookRotation(camForward);
                rotateTarget.rotation = Quaternion.Slerp(rotateTarget.rotation, aimRot, _rotationSpeed * Time.deltaTime);
            }

            TrySendMove(isMoving: false, isDodging: false);
            return;
        }

        Vector3 moveDir = (camForward * input.y + camRight * input.x).normalized;

        Quaternion targetRotation;
        if (_isAiming)
        {
            // 조준 중: 이동 방향과 무관하게 카메라 수평 방향으로 회전
            targetRotation = Quaternion.LookRotation(camForward);
        }
        else
        {
            // 비조준: 이동 방향으로 회전
            targetRotation = Quaternion.LookRotation(moveDir);
        }

        rotateTarget.rotation = Quaternion.Slerp(rotateTarget.rotation, targetRotation, _rotationSpeed * Time.deltaTime);

        // 조준 중: 카메라 기준 입력 방향으로 직접 이동 (스트레이프)
        // 비조준: PlayerModel이 바라보는 방향으로 이동
        Vector3 moveVelocity = _isAiming ? moveDir : rotateTarget.forward;
        float currentSpeed   = _moveSpeed * (IsDashing ? _dashMultiplier : 1f);
        transform.position += moveVelocity * currentSpeed * Time.deltaTime;

        TrySendMove(isMoving: true, isDodging: false);
    }


    // === 이동 동기화 ===

    // 20Hz 주기 + Dead Zone 필터: 변화가 없으면 전송 생략
    // 상태(IsMoving, IsDodging) 변화는 Dead Zone 무시하고 즉시 전송
    private void TrySendMove(bool isMoving, bool isDodging)
    {
        if (GameManager.Instance.CurrentState != GameState.Playing) return;

        bool stateChanged = isMoving != _lastSentIsMoving || isDodging != _lastSentIsDodging;
        if (!stateChanged && Time.time - _lastSendTime < SEND_INTERVAL) return;

        float rotY = (_playerModel != null ? _playerModel : transform).eulerAngles.y;
        bool posChanged = Vector3.Distance(transform.position, _lastSentPos) >= POS_THRESHOLD;
        bool rotChanged = Mathf.Abs(Mathf.DeltaAngle(rotY, _lastSentRotY)) >= ROT_THRESHOLD;

        if (!stateChanged && !posChanged && !rotChanged) return;

        NetworkManager.Instance.SendGame(GamePacketId.CMove, new C_Move
        {
            PosX      = transform.position.x,
            PosY      = transform.position.y,
            PosZ      = transform.position.z,
            RotY      = rotY,
            IsMoving  = isMoving,
            IsDodging = isDodging,
        });

        _lastSentPos       = transform.position;
        _lastSentRotY      = rotY;
        _lastSentIsMoving  = isMoving;
        _lastSentIsDodging = isDodging;
        _lastSendTime      = Time.time;
    }

    private bool CanDodge()
    {
        return Time.time - _lastDodgeTime >= _dodgeCooldown;
    }


    private IEnumerator DodgeCoroutine()
    {
        _isDodging = true;
        _lastDodgeTime = Time.time;

        // 무적 프레임 시작
        if (_stats != null) _stats.IsInvincible = true;

        PlayerEvents.DodgePerformed();

        // PlayerModel이 있으면 그 forward, 없으면 PlayerRoot forward
        Transform dirSource = _playerModel != null ? _playerModel : transform;
        Vector3 dodgeDir = dirSource.forward;

        float elapsed = 0f;
        float startTime = Time.time;

        TrySendMove(isMoving: false, isDodging: true);

        while (elapsed < _dodgeDuration)
        {
            elapsed = Time.time - startTime;
            // 초반에 빠르고 후반에 느려지는 커브로 자연스러운 닷지 느낌
            float t = 1f - (elapsed / _dodgeDuration);
            transform.position += dodgeDir * (_dodgeDistance * t * Time.deltaTime / _dodgeDuration);
            TrySendMove(isMoving: false, isDodging: true);
            yield return null;
        }

        // 무적 프레임 종료
        if (_stats != null) _stats.IsInvincible = false;
        _isDodging = false;
        TrySendMove(isMoving: false, isDodging: false);
    }
}
