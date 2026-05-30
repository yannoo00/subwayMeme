using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

// 카메라 도메인 전체를 관리하는 DDOL 싱글톤
// - FreeLook/Aim 가상카메라와 CinemachineBrain을 자식으로 보유한 채 씬 전환 시 살아남음
// - 로컬 플레이어 스폰 시 플레이어 하위 CameraHolder를 찾아 vcam의 Follow/LookAt에 바인딩
// - 마우스 입력을 받아 CameraHolder를 회전시킴 (ThirdPersonFollow가 이 회전을 기준으로 카메라 배치)
// - 조준 상태에 따라 두 vcam의 Priority를 전환
public class CameraSystem : MonoBehaviour
{
    public static CameraSystem Instance { get; private set; }

    [Header("Target")]
    [Tooltip("플레이어 루트 하위에서 찾을 카메라 피벗 오브젝트 이름")]
    [SerializeField] private string _cameraHolderName = "CameraHolder";

    [Header("Sensitivity")]
    [SerializeField] private float _sensitivityX = 0.15f;
    [SerializeField] private float _sensitivityY = 0.12f;
    [SerializeField] private float _aimSensitivityMultiplier = 0.5f;

    [Header("Vertical Clamp")]
    [SerializeField] private float _minVerticalAngle = -80f;
    [SerializeField] private float _maxVerticalAngle = 80f;

    [Header("Virtual Cameras")]
    [SerializeField] private CinemachineCamera _freeLookCamera;
    [SerializeField] private CinemachineCamera _aimCamera;

    [Header("Camera Priority")]
    [SerializeField] private int _activePriority   = 10;
    [SerializeField] private int _inactivePriority = 0;

    private Transform _cameraPivot;
    private float _xRotation;
    private float _yRotation;
    private bool  _isAiming;

    // Shake 상태 - magnitude(도) 가 시간에 따라 선형 감쇠. 매 프레임 random 오프셋을 회전에 가산
    // pivot 회전은 매 Update 에서 _xRotation/_yRotation 으로 덮어쓰여지므로, 별도 누적 회전이 아닌
    // "이번 프레임 적용 오프셋" 만 더하는 방식이라 자연스럽게 원위치로 복귀함
    private float _shakeMagnitude;
    private float _shakeDecayPerSec;


    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        PlayerEvents.OnLocalPlayerSpawned += OnLocalPlayerSpawned;
        PlayerEvents.OnAimStateChanged    += OnAimStateChanged;
        PlayerEvents.OnSpectateTargetChanged += OnSpectateTargetChanged;

        // DDOL 특성상 이미 로컬 플레이어가 스폰된 뒤에 이 컴포넌트가 활성화되는 경우가 있음
        // PlayerEvents가 마지막 Transform을 보관하고 있으므로 즉시 바인딩
        if (PlayerEvents.LocalPlayerTransform != null)
            OnLocalPlayerSpawned(PlayerEvents.LocalPlayerTransform);
        else
            SetCameraPriority(false);
    }

    private void OnDisable()
    {
        PlayerEvents.OnLocalPlayerSpawned -= OnLocalPlayerSpawned;
        PlayerEvents.OnAimStateChanged    -= OnAimStateChanged;
        PlayerEvents.OnSpectateTargetChanged -= OnSpectateTargetChanged;
    }

    private void Update()
    {
        // Playing 이외(메뉴/씬로딩/입장중/End)에는 카메라 회전 입력 차단
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return;
        if (Mouse.current == null) return;
        if (_cameraPivot == null) return;

        float sensX = _isAiming ? _sensitivityX * _aimSensitivityMultiplier : _sensitivityX;
        float sensY = _isAiming ? _sensitivityY * _aimSensitivityMultiplier : _sensitivityY;

        _yRotation += Mouse.current.delta.x.ReadValue() * sensX;
        _xRotation -= Mouse.current.delta.y.ReadValue() * sensY;
        _xRotation  = Mathf.Clamp(_xRotation, _minVerticalAngle, _maxVerticalAngle);

        Vector2 shakeOffset = Vector2.zero;
        if (_shakeMagnitude > 0f)
        {
            // insideUnitCircle 는 [-1,1] 원판 내 균등분포. 매 프레임 다른 방향이라 흔들리는 느낌이 남
            shakeOffset = UnityEngine.Random.insideUnitCircle * _shakeMagnitude;
            _shakeMagnitude = Mathf.Max(0f, _shakeMagnitude - _shakeDecayPerSec * Time.deltaTime);
        }

        _cameraPivot.rotation = Quaternion.Euler(_xRotation + shakeOffset.y, _yRotation + shakeOffset.x, 0f);
    }

    // 일회성 카메라 흔들림 - HitscanWeaponEffects 등 표현 컴포넌트에서 호출
    // magnitudeDeg: 시작 시점의 최대 각도 흔들림(도). duration 동안 선형 감쇠
    // 연속 호출되면 강한 쪽이 살아남도록 Max 합성 (사격 연사 중에도 점차 약해지지 않게)
    public void Shake(float magnitudeDeg, float duration)
    {
        if (magnitudeDeg <= 0f || duration <= 0f) return;
        _shakeMagnitude = Mathf.Max(_shakeMagnitude, magnitudeDeg);
        _shakeDecayPerSec = magnitudeDeg / duration;
    }

    private void OnLocalPlayerSpawned(Transform playerRoot)
    {
        if (playerRoot == null) return;

        _cameraPivot = FindChildRecursive(playerRoot, _cameraHolderName);
        if (_cameraPivot == null)
        {
            Debug.LogWarning($"[CameraSystem] '{_cameraHolderName}' 를 플레이어 하위에서 찾지 못했습니다. 플레이어 루트를 대신 사용합니다.");
            _cameraPivot = playerRoot;
        }

        _yRotation = _cameraPivot.eulerAngles.y;
        _xRotation = _cameraPivot.eulerAngles.x;

        BindVirtualCameras(_cameraPivot);
        SetCameraPriority(_isAiming);
    }

    private void OnAimStateChanged(bool isAiming, int weaponTypeID)
    {
        _isAiming = isAiming;
        SetCameraPriority(isAiming);
    }

    // 로컬 플레이어 사망 후 관전 대상이 바뀔 때마다 호출
    // 카메라 피벗을 관전 대상의 CameraHolder로 재바인딩 - 마우스 회전(Update)이 그대로 동작해 자유 시점이 됨
    private void OnSpectateTargetChanged(NetworkPlayer target)
    {
        if (target == null) return;

        _cameraPivot = FindChildRecursive(target.transform, _cameraHolderName);
        if (_cameraPivot == null)
        {
            Debug.LogWarning($"[CameraSystem] 관전 대상에서 '{_cameraHolderName}'를 찾지 못했습니다. 대상 루트를 사용합니다.");
            _cameraPivot = target.transform;
        }

        _yRotation = _cameraPivot.eulerAngles.y;
        _xRotation = _cameraPivot.eulerAngles.x;

        BindVirtualCameras(_cameraPivot);
    }

    private void BindVirtualCameras(Transform target)
    {
        if (_freeLookCamera != null)
        {
            _freeLookCamera.Follow = target;
            _freeLookCamera.LookAt = target;
        }

        if (_aimCamera != null)
        {
            _aimCamera.Follow = target;
            _aimCamera.LookAt = target;
        }
    }

    private void SetCameraPriority(bool isAiming)
    {
        if (_freeLookCamera != null)
            _freeLookCamera.Priority = isAiming ? _inactivePriority : _activePriority;

        if (_aimCamera != null)
            _aimCamera.Priority = isAiming ? _activePriority : _inactivePriority;
    }

    // 플레이어 프리팹에서 CameraHolder가 루트 직속이 아니라 더 깊은 곳에 있을 수도 있어 재귀 탐색
    private static Transform FindChildRecursive(Transform parent, string targetName)
    {
        if (parent.name == targetName) return parent;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform found = FindChildRecursive(parent.GetChild(i), targetName);
            if (found != null) return found;
        }
        return null;
    }
}
