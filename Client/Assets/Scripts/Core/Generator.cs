using GameProto;
using UnityEngine;

// 각 Station에 배치되는 발전기
// 파괴되면 게임 오버
// 태그: "Generator"
//
// HP 권위: 서버. 호스트가 Start 에서 C_GeneratorInit 으로 max_hp 통보 →
// 적이 발전기를 때리면 호스트가 C_EnemyAttack(target_type=GENERATOR) 송신 →
// 서버가 ApplyGeneratorDamage 후 S_GeneratorDamaged 브로드캐스트 →
// 모든 클라가 ApplyServerDamage 로 currentHp 갱신.
public class Generator : MonoBehaviour, IDamageable
{
    [Header("Stats")]
    [SerializeField] private int _maxHp = 500;

    private int _currentHp;

    // === IDamageable ===

    public int  CurrentHealth => _currentHp;
    public int  MaxHealth     => _maxHp;
    public bool IsAlive       => _currentHp > 0;

    // === Unity 생명주기 ===

    private void Awake()
    {
        _currentHp = _maxHp;
    }

    private void Start()
    {
        // 씬 시작 시 HUD에 초기 HP 알림
        GameEvents.GeneratorDamaged(_currentHp, _maxHp);

        // 호스트만 서버에 max_hp 통보 - 서버가 발전기 HP 권위를 잡도록
        if (NetworkManager.Instance != null && NetworkManager.Instance.IsHost)
        {
            NetworkManager.Instance.SendGame(GamePacketId.CGeneratorInit, new C_GeneratorInit
            {
                MaxHp = _maxHp,
            });
        }
    }

    // === IDamageable 구현 ===

    // 서버 권위 전환 후 이 경로는 호출되지 않아야 함.
    // 호출되면 코드 어딘가가 옛 로컬 데미지 패턴으로 남아 있는 것이라 경고만 남기고 무시.
    public void TakeDamage(int damage)
    {
        Debug.LogWarning($"[Generator] TakeDamage({damage}) 직접 호출 - 서버 권위 흐름을 거치지 않은 호출");
    }

    public void Die()
    {
        GameManager.Instance.EndGame();
    }

    // === 서버 권위 적용 ===

    // S_GeneratorDamaged 수신 시 호출. 서버가 확정한 currentHp 로 갱신.
    public void ApplyServerDamage(int damage, int currentHp)
    {
        if (!IsAlive) return;

        _currentHp = currentHp;
        GameEvents.GeneratorDamaged(_currentHp, _maxHp);
    }

    // S_GeneratorDestroyed 수신 시 호출. HP 를 0 으로 강제 후 Die.
    // ApplyServerDamage 와 따로 받는 이유: 서버가 데미지/파괴를 별도 패킷으로 보내므로
    // 순서 보장이 없어도 멱등하게 처리되도록 분리.
    public void ApplyServerDestroyed()
    {
        if (!IsAlive) return;

        _currentHp = 0;
        GameEvents.GeneratorDamaged(_currentHp, _maxHp);
        Die();
    }
}
