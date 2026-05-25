using UnityEngine;

// Animator 파라미터 해시 상수 모음
// 문자열 하드코딩 대신 이 클래스를 통해 참조
// StringToHash는 앱 시작 시 1회만 계산되므로 매 프레임 문자열 변환보다 성능상 유리
public static class AnimatorParams
{
    // 플레이어 이동 - Speed Blend Tree 입력 (0=idle, 1=walk, 2=dash)
    // 로컬/원격 모두 사용 (S_Move 패킷의 Speed 필드를 NetworkPlayer가 그대로 전달)
    public static readonly int Speed             = Animator.StringToHash("Speed");
    // 조준 중 스트레이프 Blend Tree 입력 (-1~1). 비조준 시에는 0으로 수렴
    public static readonly int StrafeX           = Animator.StringToHash("StrafeX");
    public static readonly int StrafeY           = Animator.StringToHash("StrafeY");

    public static readonly int Attack            = Animator.StringToHash("Attack");
    public static readonly int IsDead            = Animator.StringToHash("IsDead");
    public static readonly int IsReloading       = Animator.StringToHash("IsReloading");
    public static readonly int IsSwitchingWeapon = Animator.StringToHash("IsSwitchingWeapon");
    public static readonly int IsAiming          = Animator.StringToHash("IsAiming");

    // 적(Enemy) 전용 토글 - EnemyAnimator 가 Idle/Move 상태 전환에 사용
    public static readonly int IsMoving          = Animator.StringToHash("IsMoving");
    public static readonly int Hit              = Animator.StringToHash("Hit");   
    public static readonly int WeaponTypeID      = Animator.StringToHash("WeaponTypeID");
}
