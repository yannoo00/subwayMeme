using UnityEngine;



public enum WeaponType
{
    Melee,
    Ranged
}




[CreateAssetMenu(fileName = "WeaponData", menuName = "Scriptable Objects/WeaponData")]
public class WeaponData : ScriptableObject
{
    [Header("기본 정보")]
    public string weaponName;
    public WeaponType weaponType;


    [Header("전투 스탯")]
    public int damage;
    public float attackCooldown = 0.5f;
    public float range = 2f;

    [Header("근접 전용")]
    public float attackAngle = 90f;

    [Header("원거리 전용")]
    public GameObject projectilePrefab;
    public float projectileSpeed = 20f;
}
