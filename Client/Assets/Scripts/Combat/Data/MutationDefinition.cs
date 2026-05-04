using UnityEngine;

public enum MutationType { StatBoost, ActiveSkill, PassiveBehavior }

public enum StatType { MaxHp, AttackDamage, MoveSpeed }

[CreateAssetMenu(fileName = "MutationDefinition", menuName = "Game/Mutation Definition")]
public class MutationDefinition : ScriptableObject
{
    [Header("기본 정보")]
    public string mutationName;
    [TextArea] public string description;

    [Header("타입")]
    public MutationType mutationType;

    [Header("StatBoost 전용")]
    public StatType statType;
    public float    value;

    [Header("ActiveSkill / PassiveBehavior 전용")]
    public GameObject effectPrefab;
}
