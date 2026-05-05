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

    [Header("드랍")]
    // Elite 전용: 처치 시 무조건 드랍. type은 반드시 Mutation으로 설정
    public DropEntry eliteDrop;

    // Normal 전용: dropChance 확률 판정 후 dropTable 가중치 룰렛
    [Range(0f, 1f)] public float dropChance;
    public DropEntry[] dropTable;
}
