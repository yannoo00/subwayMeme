using GameProto;
using UnityEngine;

public enum MutationType
{
    StatBoost = 0,
    ActiveSkill = 1,
    PassiveBehavior = 2
}

public enum StatType
{
    MaxHp = 0,
    AttackDamage = 1,
    MoveSpeed = 2
}

[CreateAssetMenu(fileName = "MutationDefinition", menuName = "Game/Mutation Definition")]
public class MutationDefinition : ScriptableObject
{
    [Header("기본 정보")]
    public string mutationName;
    [TextArea] public string description;

    // 서버/클라 동기화에 쓰이는 식별자. game.proto 의 MutationId enum 값과 1:1 매칭
    [Header("동기화 ID")]
    public MutationId mutationId;

    [Header("타입")]
    public MutationType mutationType;

    [Header("StatBoost 전용")]
    public StatType statType;
    public float    value;

    [Header("ActiveSkill / PassiveBehavior 전용")]
    public GameObject effectPrefab;
}
