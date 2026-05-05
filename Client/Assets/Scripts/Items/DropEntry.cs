using UnityEngine;

public enum DropType { Mutation, EvolutionPoint, GenePoint, Weapon }

[System.Serializable]
public class DropEntry
{
    public DropType type;

    // 가중치 룰렛에 사용 (Elite 고정 드랍에서는 무시됨)
    [Min(0f)] public float weight = 1f;

    // type == Mutation
    public MutationDefinition mutation;

    // type == EvolutionPoint or GenePoint
    public int pointAmount = 10;

    // type == Weapon: WeaponData.pickupPrefab 과 동일한 WeaponPickup 프리팹
    public GameObject weaponPickupPrefab;
}
