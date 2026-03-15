using UnityEngine;

// Animator 파라미터 해시 상수 모음
// 문자열 하드코딩 대신 이 클래스를 통해 참조
// StringToHash는 앱 시작 시 1회만 계산되므로 매 프레임 문자열 변환보다 성능상 유리
public static class AnimatorParams
{
    public static readonly int IsMoving = Animator.StringToHash("IsMoving");
    public static readonly int Attack   = Animator.StringToHash("Attack");
    public static readonly int IsDead   = Animator.StringToHash("IsDead");
    public static readonly int Dodge    = Animator.StringToHash("Dodge");
}
