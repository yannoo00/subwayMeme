using UnityEngine;


[CreateAssetMenu(fileName = "HitscanWeaponData", menuName = "Scriptable Objects/Weapons/Hitscan")]
public class HitscanWeaponData : WeaponData
{
    [Header("탄약")]
    public int magazineSize = 12;
    public float reloadTime = 1.5f;

    [Header("확산")]
    public float spread = 0f;           // 0이면 확산 없음. 카메라 forward를 중심으로 좌우/상하로 흩뿌림

    public override WeaponType weaponType => WeaponType.Ranged;
}
