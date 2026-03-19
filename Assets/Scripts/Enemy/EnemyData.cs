using UnityEngine;




[CreateAssetMenu(fileName = "EnemyData", menuName = "Scriptable Objects/EnemyData")]
public class EnemyData : ScriptableObject
{
    public string   enemyName;
    public int      maxHealth;
    public float    moveSpeed;
    public float    detectionRange;
    public float    attackRange;
    public int      attackDamage;
    public float    attackCooldown;
}
