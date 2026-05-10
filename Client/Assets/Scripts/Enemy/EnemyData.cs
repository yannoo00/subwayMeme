using UnityEngine;

public enum EnemyGrade { Normal, Elite, Boss }

[CreateAssetMenu(fileName = "EnemyData", menuName = "Scriptable Objects/EnemyData")]
public class EnemyData : ScriptableObject
{
    [Header("기본 스탯")]
    public string   enemyName;
    public int      maxHealth;
    public float    moveSpeed;
    public float    detectionRange;
    public float    attackRange;
    public int      attackDamage;
    public float    attackCooldown;

    [Header("등급")]
    public EnemyGrade grade;

    [Header("드롭 - 재화")]
    [Range(0f, 1f)] public float evolutionPointChance;
    public int evolutionPointAmount;
    [Range(0f, 1f)] public float genePointChance;
    public int genePointAmount;

    [Header("드롭 - 변이 (Elite 전용)")]
    public MutationDefinition eliteDrop;
}
