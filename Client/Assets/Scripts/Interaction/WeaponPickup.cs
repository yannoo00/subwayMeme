using UnityEngine;

// 바닥에 놓인 줍기 가능한 무기 오브젝트
// PlayerInteractor가 범위 안에서 E키 감지 시 Interact() 호출
public class WeaponPickup : MonoBehaviour, IInteractable
{
    [SerializeField] private GameObject _weaponPrefab;

    // === IInteractable ===

    public void Interact()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        var combat = player.GetComponent<PlayerCombat>();
        if (combat == null) return;

        combat.TryPickupWeapon(_weaponPrefab);
        Destroy(gameObject);
    }

    public string GetHintText()
    {
        var wb = _weaponPrefab != null
            ? _weaponPrefab.GetComponent<WeaponBase>()
            : null;

        string name = wb?.weaponData?.weaponName ?? "무기";
        return $"E: {name} 줍기";
    }
}
