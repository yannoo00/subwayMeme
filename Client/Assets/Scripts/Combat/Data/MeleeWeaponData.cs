using UnityEngine;


[CreateAssetMenu(fileName = "MeleeWeaponData", menuName = "Scriptable Objects/Weapons/Melee")]
public class MeleeWeaponData : WeaponData
{
    [Header("근접 전용")]
    public float attackAngle = 90f;
    public bool isSplash = false;

    public override WeaponType weaponType => WeaponType.Melee;
}
