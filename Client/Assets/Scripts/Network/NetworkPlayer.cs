using UnityEngine;

// 원격 플레이어 오브젝트에 붙는 컴포넌트
// S_Move 수신 시 SetTargetState()로 목표 상태를 갱신하고
// Update()에서 Lerp/Slerp로 부드럽게 보간
public class NetworkPlayer : MonoBehaviour
{
    public int    PlayerId   { get; private set; }
    public string PlayerName { get; private set; }
    public bool IsAlive = true;

    private Animator  _animator;
    private Transform _modelTransform;

    private Vector3 _targetPos;
    private float   _targetRotY;
    private bool    _isDodging;

    // === 초기화 ===

    public void Init(int playerId, string playerName)
    {
        PlayerId   = playerId;
        PlayerName = playerName;
        gameObject.name = $"RemotePlayer_{playerId}_{playerName}";

        _animator       = GetComponentInChildren<Animator>();
        _modelTransform = transform.Find("PlayerModel") ?? transform;
        _targetPos      = transform.position;
    }

    // === S_Move 수신 시 호출 ===

    public void SetTargetState(Vector3 pos, float rotY, bool isMoving, bool isDodging)
    {
        _targetPos  = pos;
        _targetRotY = rotY;

        // 닷지 시작 시점에만 트리거 (연속 호출 방지)
        if (!_isDodging && isDodging)
            _animator?.SetTrigger(AnimatorParams.Dodge);

        _isDodging = isDodging;
        _animator?.SetBool(AnimatorParams.IsMoving, isMoving);
    }

    // S_Attack 수신 시 호출 - 다른 플레이어의 공격 애니메이션 재생
    public void TriggerAttackAnim()
    {
        _animator?.SetTrigger(AnimatorParams.Attack);
    }

    // === 보간 ===

    private void Update()
    {
        // 위치: 50ms 주기 패킷 사이에 자연스럽게 도달하는 속도
        transform.position = Vector3.Lerp(transform.position, _targetPos, 15f * Time.deltaTime);

        // 회전: PlayerModel만 회전 (PlayerRoot는 카메라와 공유하지 않으므로 여기선 동일)
        Quaternion targetRot = Quaternion.Euler(0f, _targetRotY, 0f);
        _modelTransform.rotation = Quaternion.Slerp(_modelTransform.rotation, targetRot, 15f * Time.deltaTime);
    }
}
