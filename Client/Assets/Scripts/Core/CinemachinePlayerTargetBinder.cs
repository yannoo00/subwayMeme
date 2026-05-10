using Unity.Cinemachine;
using UnityEngine;

// Cinemachine vcam의 Follow/LookAt을 동적으로 로컬 플레이어 Transform에 바인딩한다.
//
// 로컬 플레이어가 PlayerRegistry에서 동적 스폰되므로 씬에 baked된 vcam의 Tracking Target은 끊어진 상태.
// PlayerEvents.OnLocalPlayerSpawned 를 받아서 자동으로 wire up.
//
// 사용법: 각 vcam GameObject(CinemachineCamera 컴포넌트가 있는 곳)에 이 컴포넌트를 추가.
// Aim vcam처럼 Follow만 필요하고 LookAt은 다른 곳을 쓰는 경우에는 _setLookAt = false 로 끔.
[RequireComponent(typeof(CinemachineVirtualCameraBase))]
public class CinemachinePlayerTargetBinder : MonoBehaviour
{
    [Header("Tracking")]
    [Tooltip("로컬 플레이어 스폰 시 Follow에 할당. 끄면 Follow는 건드리지 않음.")]
    [SerializeField] private bool _setFollow = true;

    [Tooltip("로컬 플레이어 스폰 시 LookAt에 할당. 끄면 LookAt은 건드리지 않음.")]
    [SerializeField] private bool _setLookAt = true;

    private CinemachineVirtualCameraBase _vcam;


    private void Awake()
    {
        _vcam = GetComponent<CinemachineVirtualCameraBase>();
    }

    private void OnEnable()
    {
        PlayerEvents.OnLocalPlayerSpawned += OnLocalPlayerSpawned;

        // 이미 스폰된 상태에서 vcam이 활성화되는 케이스 (씬 전환 후 새 vcam 등장) 즉시 wire up
        if (PlayerEvents.LocalPlayerTransform != null)
            ApplyTarget(PlayerEvents.LocalPlayerTransform);
    }

    private void OnDisable()
    {
        PlayerEvents.OnLocalPlayerSpawned -= OnLocalPlayerSpawned;
    }


    private void OnLocalPlayerSpawned(Transform t)
    {
        ApplyTarget(t);
    }

    private void ApplyTarget(Transform t)
    {
        if (_vcam == null) return;
        if (_setFollow) _vcam.Follow = t;
        if (_setLookAt) _vcam.LookAt = t;
    }
}
