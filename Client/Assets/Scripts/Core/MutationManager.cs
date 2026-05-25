using System.Collections.Generic;
using GameProto;
using UnityEngine;

// 런 내 변이 관리 싱글톤
// 픽업 상호작용 시 C_Mutation 송신만 하고, 서버 응답(S_MutationResult) 시점에 실제 효과 적용
// 본인: PlayerStats / PlayerCombat / PassiveBase 에 적용
// 남: PassiveBehavior 변이의 effectPrefab 만 NetworkPlayer 자식으로 부착 (외관 표현)
public class MutationManager : MonoBehaviour
{
    public static MutationManager Instance { get; private set; }

    [SerializeField] private MutationDefinition[] _mutations;
    private Dictionary<MutationId, MutationDefinition> _lookup;

    // 플레이어별 획득 변이 목록 (UI 표시 / 디버깅용)
    private readonly Dictionary<int, List<MutationDefinition>> _activeMutations = new();

    // 본인 한정 ActiveSkill 슬롯 카운터 (0 or 1)
    private int _myNextSkillSlot = 0;

    public IReadOnlyDictionary<int, List<MutationDefinition>> ActiveMutations => _activeMutations;


    // === Unity 생명주기 ===

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        BuildLookup();
    }


    // === 외부 진입점 ===

    // DropItemPickup 에서 호출. 효과는 서버 응답 시점에 적용되므로 여기서는 송신만 한다
    public void ApplyMutation(MutationDefinition mutation)
    {
        if (mutation == null) return;

        NetworkManager.Instance.SendGame(GamePacketId.CMutation, new C_Mutation
        {
            MutationId = mutation.mutationId,
        });
    }

    // ClientGamePacketHandler.Handle_S_MutationResult 에서 호출
    // 서버가 확정한 변이를 본인/남에게 분기 적용
    public void ApplyMutationFromServer(int playerId, MutationId mutationId)
    {
        if (_lookup == null) BuildLookup();
        _lookup.TryGetValue(mutationId, out var mutation);
        if (mutation == null)
        {
            Debug.LogWarning($"[MutationManager] 알 수 없는 변이 ID: {mutationId}");
            return;
        }

        if (!_activeMutations.TryGetValue(playerId, out var list))
        {
            list = new List<MutationDefinition>();
            _activeMutations[playerId] = list;
        }
        list.Add(mutation);

        bool isMe = playerId == NetworkManager.Instance.MyPlayerId;
        Transform target = ResolvePlayerTransform(playerId, isMe);
        if (target == null)
        {
            Debug.LogWarning($"[MutationManager] playerId={playerId} 의 오브젝트를 찾지 못했습니다.");
            return;
        }

        if (isMe)
        {
            switch (mutation.mutationType)
            {
                case MutationType.StatBoost:
                    ApplyStatBoost(target.gameObject, mutation);
                    break;
                case MutationType.ActiveSkill:
                    ApplyActiveSkill(target.gameObject, mutation);
                    break;
                case MutationType.PassiveBehavior:
                    ApplyPassiveBehavior(target, mutation);
                    break;
            }
        }
        else
        {
            // 남의 변이는 외관 표현만. StatBoost / ActiveSkill 은 시각 변화 없음
            // PassiveBehavior 의 effectPrefab 은 본인 클라가 자기 PlayerStats 등을 참조하지 않는 한
            // 다른 플레이어 오브젝트 자식으로 부착돼도 시각만 보여진다
            if (mutation.mutationType == MutationType.PassiveBehavior)
                ApplyPassiveBehavior(target, mutation);
        }

        Debug.Log($"[MutationManager] 변이 적용: playerId={playerId}, name={mutation.mutationName} ({mutation.mutationType})");
    }


    // === 변이별 적용 ===

    private void ApplyStatBoost(GameObject player, MutationDefinition mutation)
    {
        var stats = player.GetComponent<PlayerStats>();
        if (stats == null) return;

        stats.ApplyStatBoost(mutation.statType, mutation.value);
    }

    private void ApplyActiveSkill(GameObject player, MutationDefinition mutation)
    {
        if (_myNextSkillSlot >= 2)
        {
            Debug.LogWarning("[MutationManager] 스킬 슬롯이 가득 찼습니다. (최대 2개)");
            return;
        }

        if (mutation.effectPrefab == null)
        {
            Debug.LogWarning($"[MutationManager] {mutation.mutationName} effectPrefab이 없습니다.");
            return;
        }

        var combat = player.GetComponent<PlayerCombat>();
        if (combat == null) return;

        combat.AssignSkill(_myNextSkillSlot, mutation.effectPrefab);
        _myNextSkillSlot++;
    }

    private void ApplyPassiveBehavior(Transform parent, MutationDefinition mutation)
    {
        if (mutation.effectPrefab == null)
        {
            Debug.LogWarning($"[MutationManager] {mutation.mutationName} effectPrefab이 없습니다.");
            return;
        }

        Instantiate(mutation.effectPrefab, parent);
    }


    // === 런 초기화 ===

    // 새 런 시작 시 호출 - 변이 목록 및 스킬 슬롯 초기화
    public void ResetForNewRun()
    {
        _activeMutations.Clear();
        _myNextSkillSlot = 0;

        // 본인의 변이만 리셋 - RemotePlayer 의 PassiveBase 는 그 플레이어의 클라이언트가 알아서 처리
        var player = PlayerRegistry.Instance != null ? PlayerRegistry.Instance.LocalPlayer : null;
        if (player == null) return;

        player.GetComponent<PlayerStats>()?.ResetMutationBonuses();

        // 플레이어에 붙어있는 PassiveBase 컴포넌트 제거
        foreach (var passive in player.GetComponentsInChildren<PassiveBase>())
            Destroy(passive.gameObject);
    }


    // === 데이터 조회 ===

    private void BuildLookup()
    {
        _lookup = new Dictionary<MutationId, MutationDefinition>();
        if (_mutations == null) return;

        foreach (var m in _mutations)
        {
            if (m == null) continue;
            if (_lookup.ContainsKey(m.mutationId))
            {
                Debug.LogError($"[MutationManager] 중복 ID: {m.mutationId} ({m.mutationName})");
                continue;
            }
            _lookup[m.mutationId] = m;
        }
    }


    // === 헬퍼 ===

    private Transform ResolvePlayerTransform(int playerId, bool isMe)
    {
        if (isMe)
        {
            // FindGameObjectWithTag 는 RemotePlayer 도 잡힐 수 있으므로 PlayerRegistry 의 LocalPlayer 사용
            var go = PlayerRegistry.Instance != null ? PlayerRegistry.Instance.LocalPlayer : null;
            return go != null ? go.transform : null;
        }
        var np = PlayerRegistry.Instance != null ? PlayerRegistry.Instance.Get(playerId) : null;
        return np != null ? np.transform : null;
    }
}
