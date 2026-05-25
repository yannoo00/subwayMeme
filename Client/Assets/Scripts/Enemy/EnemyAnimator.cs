using UnityEngine;

// 적 애니메이션 전담 컴포넌트
// 다른 컴포넌트는 AnimatorParams를 몰라도 되고, 의도만 전달하면 됨
// 애니메이션 파라미터가 바뀌어도 이 파일만 수정

public class EnemyAnimator : MonoBehaviour
{
    private Animator _animator;


    private void Awake()
    {
        _animator = GetComponentInChildren<Animator>();

        if (_animator == null)
            Debug.LogWarning($"[EnemyAnimator] {gameObject.name} 에 Animator가 없습니다.");
    }


    public void PlayIdle()   => _animator?.SetBool(AnimatorParams.IsMoving, false);
    public void PlayMove()   => _animator?.SetBool(AnimatorParams.IsMoving, true);
    public void PlayAttack() => _animator?.SetTrigger(AnimatorParams.Attack);
    public void PlayDeath()  => _animator?.SetBool(AnimatorParams.IsDead, true);
    public void PlayHit()    => _animator?.SetTrigger(AnimatorParams.Hit);
}
