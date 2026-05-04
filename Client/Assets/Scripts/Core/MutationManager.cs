using System.Collections.Generic;
using UnityEngine;

// 런 내 변이 관리 싱글톤
// 변이 획득 시 타입에 따라 PlayerStats / PlayerCombat / PassiveBase 에 적용
public class MutationManager : MonoBehaviour
{
    public static MutationManager Instance { get; private set; }

    private readonly List<MutationDefinition> _activeMutations = new();

    // ActiveSkill 배정 시 다음 빈 슬롯 추적 (0 or 1)
    private int _nextSkillSlot = 0;

    public IReadOnlyList<MutationDefinition> ActiveMutations => _activeMutations;


    // === Unity 생명주기 ===

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }


    // === 변이 적용 ===

    // 변이 선택 UI 또는 드롭 상호작용에서 호출
    public void ApplyMutation(MutationDefinition mutation)
    {
        if (mutation == null) return;

        _activeMutations.Add(mutation);

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogWarning("[MutationManager] Player 태그 오브젝트를 찾지 못했습니다.");
            return;
        }

        switch (mutation.mutationType)
        {
            case MutationType.StatBoost:
                ApplyStatBoost(player, mutation);
                break;

            case MutationType.ActiveSkill:
                ApplyActiveSkill(player, mutation);
                break;

            case MutationType.PassiveBehavior:
                ApplyPassiveBehavior(player, mutation);
                break;
        }

        Debug.Log($"[MutationManager] 변이 적용: {mutation.mutationName} ({mutation.mutationType})");
    }

    private void ApplyStatBoost(GameObject player, MutationDefinition mutation)
    {
        var stats = player.GetComponent<PlayerStats>();
        if (stats == null) return;

        stats.ApplyStatBoost(mutation.statType, mutation.value);
    }

    private void ApplyActiveSkill(GameObject player, MutationDefinition mutation)
    {
        if (_nextSkillSlot >= 2)
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

        combat.AssignSkill(_nextSkillSlot, mutation.effectPrefab);
        _nextSkillSlot++;
    }

    private void ApplyPassiveBehavior(GameObject player, MutationDefinition mutation)
    {
        if (mutation.effectPrefab == null)
        {
            Debug.LogWarning($"[MutationManager] {mutation.mutationName} effectPrefab이 없습니다.");
            return;
        }

        Instantiate(mutation.effectPrefab, player.transform);
    }


    // === 런 초기화 ===

    // 새 런 시작 시 호출 - 변이 목록 및 스킬 슬롯 초기화
    public void ResetForNewRun()
    {
        _activeMutations.Clear();
        _nextSkillSlot = 0;

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        player.GetComponent<PlayerStats>()?.ResetMutationBonuses();

        // 플레이어에 붙어있는 PassiveBase 컴포넌트 제거
        foreach (var passive in player.GetComponentsInChildren<PassiveBase>())
            Destroy(passive.gameObject);
    }
}
