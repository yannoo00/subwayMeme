using UnityEngine;


public enum WeaponType
{
    Melee,
    Ranged
}


// 무기 데이터의 추상 베이스
// 공용 필드만 보유. 무기 종류별 필드는 파생 ScriptableObject(MeleeWeaponData, HitscanWeaponData)에서 정의
public abstract class WeaponData : ScriptableObject
{
    [Header("기본 정보")]
    public string weaponName;

    [Header("전투 스탯")]
    public int damage;
    public float attackCooldown = 0.5f;
    public float range = 2f;

    [Header("픽업")]
    // 슬롯 가득 시 드랍할 때 사용. 월드에 떨어진 무기 표현 (WeaponPickup 컴포넌트 포함)
    public GameObject pickupPrefab;

    // 런타임에서 캐스팅 없이 무기 종류 판별하기 위한 마커
    public abstract WeaponType weaponType { get; }
}
