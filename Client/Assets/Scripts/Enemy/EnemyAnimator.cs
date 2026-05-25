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
        {
            Debug.LogError($"[EnemyAnimator] {gameObject.name} - Animator를 찾지 못했습니다.");
            return;
        }

        Debug.Log($"[EnemyAnimator] {gameObject.name} - Animator 찾음: {_animator.gameObject.name} / Controller: {_animator.runtimeAnimatorController?.name ?? "없음"}");
    }


    public void PlayIdle()
    {
        Debug.Log($"[EnemyAnimator] {gameObject.name} PlayIdle - IsMoving → false");
        _animator?.SetBool(AnimatorParams.IsMoving, false);
    }

    public void PlayMove()
    {
        Debug.Log($"[EnemyAnimator] {gameObject.name} PlayMove - IsMoving → true");
        _animator?.SetBool(AnimatorParams.IsMoving, true);
    }

    public void PlayAttack() => _animator?.SetTrigger(AnimatorParams.Attack);
    public void PlayDeath()  => _animator?.SetBool(AnimatorParams.IsDead, true);

    public void PlayHit()
    {
        Debug.Log($"[EnemyAnimator] {gameObject.name} PlayHit - Hit 트리거");
        _animator?.SetTrigger(AnimatorParams.Hit);
    }
}
