using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

// Cinemachine 기반 카메라 입력 처리
// CameraHolder(Follow 타겟)를 마우스로 회전시키고, 조준 상태에 따라 가상카메라를 전환함
// FOV, 어깨 오프셋은 각 CinemachineCamera Inspector 설정에서 관리
public class MouseLook : MonoBehaviour
{
    [Header("Follow Target")]
    [Tooltip("수평/수직 회전을 적용할 CameraHolder transform (Cinemachine Follow 타겟)")]
    [SerializeField] private Transform _cameraPivot;

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

    private float _xRotation; // 수직(pitch)
    private float _yRotation; // 수평(yaw)
    private bool  _isAiming;


    private void Start()
    {
        // 시작 시 CameraHolder의 현재 회전을 기준값으로 사용
        if (_cameraPivot != null)
        {
            _yRotation = _cameraPivot.eulerAngles.y;
            _xRotation = _cameraPivot.eulerAngles.x;
        }

        // 초기 카메라 우선순위: FreeLook 활성
        SetCameraPriority(isAiming: false);
    }

    private void OnEnable()
    {
        PlayerEvents.OnAimStateChanged += OnAimStateChanged;
    }

    private void OnDisable()
    {
        PlayerEvents.OnAimStateChanged -= OnAimStateChanged;
    }

    private void OnAimStateChanged(bool isAiming)
    {
        _isAiming = isAiming;
        SetCameraPriority(isAiming);
    }

    private void Update()
    {
        if (GameManager.Instance.CurrentState == GameState.Menu) return;
        if (Mouse.current == null) return;
        if (_cameraPivot == null) return;

        float sensX = _isAiming ? _sensitivityX * _aimSensitivityMultiplier : _sensitivityX;
        float sensY = _isAiming ? _sensitivityY * _aimSensitivityMultiplier : _sensitivityY;

        _yRotation += Mouse.current.delta.x.ReadValue() * sensX;
        _xRotation -= Mouse.current.delta.y.ReadValue() * sensY;
        _xRotation  = Mathf.Clamp(_xRotation, _minVerticalAngle, _maxVerticalAngle);

        // CameraHolder에 수평+수직 회전을 모두 적용
        // Cinemachine ThirdPersonFollow가 이 rotation을 기준으로 카메라를 배치함
        _cameraPivot.rotation = Quaternion.Euler(_xRotation, _yRotation, 0f);
    }

    private void SetCameraPriority(bool isAiming)
    {
        if (_freeLookCamera != null)
            _freeLookCamera.Priority = isAiming ? _inactivePriority : _activePriority;

        if (_aimCamera != null)
            _aimCamera.Priority = isAiming ? _activePriority : _inactivePriority;
    }
}
