using UnityEngine;

// 총기 발사 표현(머즐 플래시 / 카메라 흔들림) 전담 컴포넌트
// 같은 prefab 의 HitscanWeapon.OnFired 를 구독해서 동작
//
// 표현/로직 분리 이유:
// - HitscanWeapon 은 raycast, 탄약, 재장전, 네트워크 송신까지 책임이 이미 큼
// - 시각/사운드 자산은 prefab variant 단위로 바뀔 가능성이 커서 SerializeField 만 따로 묶는 게 깔끔
// - 무기 로직 디버깅 중에 prefab 가시 자산이 끼어들지 않게 함
[RequireComponent(typeof(HitscanWeapon))]
public class HitscanWeaponEffects : MonoBehaviour
{
    [Header("Muzzle Flash")]
    // muzzle Transform 하위에 자식으로 미리 배치한 ParticleSystem 을 연결
    // 이렇게 두면 위치/회전을 코드에서 따로 set 할 필요 없이 부모 따라가게 됨
    [SerializeField] private ParticleSystem _muzzleFlash;

    [Header("Camera Shake")]
    [SerializeField] private float _shakeMagnitudeDeg = 0.4f;
    [SerializeField] private float _shakeDuration = 0.08f;


    private HitscanWeapon _weapon;


    private void Awake()
    {
        _weapon = GetComponent<HitscanWeapon>();
    }

    // 이벤트 구독은 OnEnable/OnDisable - 무기 스위칭으로 GameObject 가 비활성화될 때 자동 해제
    // 다시 활성화 시 재구독되므로 메모리 누수 없음
    private void OnEnable()
    {
        if (_weapon != null) _weapon.OnFired += HandleFired;
    }

    private void OnDisable()
    {
        if (_weapon != null) _weapon.OnFired -= HandleFired;
    }


    private void HandleFired()
    {
        PlayMuzzleFlash();
        ShakeCamera();
    }

    private void PlayMuzzleFlash()
    {
        if (_muzzleFlash == null) return;
        // Stop -> Play 로 재시작. 연사 중 직전 인스턴스가 끝나기 전에 새 발사가 와도 매번 처음부터 재생
        _muzzleFlash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        _muzzleFlash.Play();
    }

    private void ShakeCamera()
    {
        CameraSystem.Instance?.Shake(_shakeMagnitudeDeg, _shakeDuration);
    }
}
