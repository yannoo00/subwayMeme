using UnityEngine;

// Animator 파라미터 해시 상수 모음
// 문자열 하드코딩 대신 이 클래스를 통해 참조
// StringToHash는 앱 시작 시 1회만 계산되므로 매 프레임 문자열 변환보다 성능상 유리
public static class AnimatorParams
{
    // 로컬 플레이어 이동 - Speed Blend Tree 입력 (0=idle, 1=walk, 2=dash)
    public static readonly int Speed             = Animator.StringToHash("Speed");
    // 조준 중 스트레이프 Blend Tree 입력 (-1~1). 비조준 시에는 0으로 수렴
    public static readonly int StrafeX           = Animator.StringToHash("StrafeX");
    public static readonly int StrafeY           = Animator.StringToHash("StrafeY");

    public static readonly int Attack            = Animator.StringToHash("Attack");
    public static readonly int IsDead            = Animator.StringToHash("IsDead");
    public static readonly int Dodge             = Animator.StringToHash("Dodge");
    public static readonly int IsReloading       = Animator.StringToHash("IsReloading");
    public static readonly int IsSwitchingWeapon = Animator.StringToHash("IsSwitchingWeapon");
    public static readonly int IsAiming          = Animator.StringToHash("IsAiming");

    // NetworkPlayer 원격 동기화용 - 로컬 플레이어는 Speed 사용 -> 이것도 수정 필요함. 원격의 속도도 동기화.
    // 서버 C_Move 패킷이 IsMoving bool만 전달하므로 원격 측은 단순 토글 유지
    // 패킷에 Speed/Strafe 추가 협의 후 NetworkPlayer 마이그레이션 가능
    public static readonly int IsMoving          = Animator.StringToHash("IsMoving");
    public static readonly int WeaponTypeID      = Animator.StringToHash("WeaponTypeID");
}
