using UnityEngine;

// 비호스트 클라이언트의 적 오브젝트에 붙는 컴포넌트
// EnemyAI가 비활성화된 상태에서 S_EnemySync 패킷으로 받은 위치/회전/상태를
// NetworkPlayer와 동일한 보간 패턴으로 적용해 적을 표현함
public class NetworkEnemy : MonoBehaviour
{
    private Vector3       _targetPos;
    private float         _targetRotY;
    private int           _targetState;

    // 불필요한 PlayIdle/PlayMove/PlayAttack 중복 호출 방지
    private int           _currentState;

    private EnemyAnimator _animator;

    // === Unity 생명주기 ===

    private void Awake()
    {
        _animator     = GetComponent<EnemyAnimator>();
        _targetPos    = transform.position;
        _currentState = -1;
    }

    private void Update()
    {
        // 위치: 50ms 주기 패킷 사이에 자연스럽게 도달하는 속도
        transform.position = Vector3.Lerp(transform.position, _targetPos, 15f * Time.deltaTime);

        // 회전
        Quaternion targetRot = Quaternion.Euler(0f, _targetRotY, 0f);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 15f * Time.deltaTime);

        // 상태가 바뀐 시점에만 애니메이션 전환 (매 프레임 호출 방지)
        if (_currentState != _targetState)
        {
            _currentState = _targetState;

            switch (_currentState)
            {
                case 0: _animator?.PlayIdle();   break;
                case 1: _animator?.PlayMove();   break;
                case 2: _animator?.PlayAttack(); break;
            }
        }
    }

    // === S_EnemySync 수신 시 호출 ===

    public void SetTargetState(Vector3 pos, float rotY, int state)
    {
        _targetPos   = pos;
        _targetRotY  = rotY;
        _targetState = state;
    }

    // === S_EnemyAttack 수신 시 호출 ===

    public void TriggerAttackAnim()
    {
        _animator?.PlayAttack();
    }
}
