using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float _moveSpeed = 5f;
    // 카메라 forward를 기준으로 이동 방향 계산
    [SerializeField] private Transform _cameraTransform;
    // 회전 대상: PlayerRoot 대신 PlayerModel만 회전시켜 카메라가 같이 돌지 않도록
    [SerializeField] private Transform _playerModel;
    // 캐릭터가 이동 방향으로 회전하는 속도
    [SerializeField] private float _rotationSpeed = 15f;

    [Header("Dodge")]
    [SerializeField] private float _dodgeDistance = 4f;
    [SerializeField] private float _dodgeDuration = 0.25f;
    [SerializeField] private float _dodgeCooldown = 1.5f;

    private Animator _animator;
    private PlayerStats _stats;
    private bool _isDodging = false;
    private float _lastDodgeTime = -Mathf.Infinity;


    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        _animator = GetComponentInChildren<Animator>();
        _stats = GetComponent<PlayerStats>();
    }


    void Update()
    {
        if (GameManager.Instance.CurrentState == GameState.Menu) return;
        if (Keyboard.current == null) return;
        if (_isDodging) return;
        if (!_stats.IsAlive) return;

        Vector2 input = new Vector2(
            (Keyboard.current.dKey.isPressed ? 1 : 0) - (Keyboard.current.aKey.isPressed ? 1 : 0),
            (Keyboard.current.wKey.isPressed ? 1 : 0) - (Keyboard.current.sKey.isPressed ? 1 : 0));

        _animator?.SetBool(AnimatorParams.IsMoving, input != Vector2.zero);

        // 스페이스바 닷지 입력 (쿨타임 체크 포함)
        if (Keyboard.current.spaceKey.wasPressedThisFrame && CanDodge())
        {
            StartCoroutine(DodgeCoroutine());
            return;
        }

        if (input == Vector2.zero) return;

        // 카메라의 수평 forward/right를 기준으로 월드 이동 방향 계산
        // Y축 성분 제거 후 정규화: 경사면에서도 수평 이동 보장
        Vector3 camForward = _cameraTransform != null ? _cameraTransform.forward : transform.forward;
        camForward.y = 0f;
        camForward.Normalize();
        Vector3 camRight = _cameraTransform != null ? _cameraTransform.right : transform.right;
        camRight.y = 0f;
        camRight.Normalize();

        Vector3 moveDir = (camForward * input.y + camRight * input.x).normalized;

        // PlayerModel만 이동 방향으로 회전 (PlayerRoot는 회전 안 함)
        // CameraHolder가 PlayerRoot 자식이므로 PlayerRoot가 돌면 카메라도 같이 돌아버림
        // Slerp: 구면 선형 보간으로 자연스러운 회전감 제공
        Transform rotateTarget = _playerModel != null ? _playerModel : transform;
        Quaternion targetRotation = Quaternion.LookRotation(moveDir);
        rotateTarget.rotation = Quaternion.Slerp(rotateTarget.rotation, targetRotation, _rotationSpeed * Time.deltaTime);

        // 이동은 PlayerRoot 위치 기준, 방향은 PlayerModel forward 참조
        transform.position += rotateTarget.forward * _moveSpeed * Time.deltaTime;
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

        _animator?.SetTrigger(AnimatorParams.Dodge);

        // PlayerModel이 있으면 그 forward, 없으면 PlayerRoot forward
        Transform dirSource = _playerModel != null ? _playerModel : transform;
        Vector3 dodgeDir = dirSource.forward;

        float elapsed = 0f;
        float startTime = Time.time;

        while (elapsed < _dodgeDuration)
        {
            elapsed = Time.time - startTime;
            // 초반에 빠르고 후반에 느려지는 커브로 자연스러운 닷지 느낌
            float t = 1f - (elapsed / _dodgeDuration);
            transform.position += dodgeDir * (_dodgeDistance * t * Time.deltaTime / _dodgeDuration);
            yield return null;
        }

        // 무적 프레임 종료
        if (_stats != null) _stats.IsInvincible = false;
        _isDodging = false;
    }
}
