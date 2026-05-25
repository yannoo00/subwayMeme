using UnityEngine;

// 바닥에 놓인 줍기 가능한 무기 오브젝트
// PlayerInteractor가 범위 안에서 E키 감지 시 Interact() 호출
public class WeaponPickup : MonoBehaviour, IInteractable
{
    [SerializeField] private GameObject _weaponPrefab;

    // === IInteractable ===

    public void Interact()
    {
        // FindGameObjectWithTag("Player") 는 RemotePlayer 도 같은 태그라 운에 따라 다른 플레이어가 잡힘.
        // 픽업은 클라이언트 귀속 동작이므로 PlayerRegistry.LocalPlayer 만 대상으로 한다.
        var player = PlayerRegistry.Instance != null ? PlayerRegistry.Instance.LocalPlayer : null;
        if (player == null)
        {
            Debug.Log("[WeaponPickup] LocalPlayer 미존재 - 픽업 무시");
            return;
        }

        var combat = player.GetComponent<PlayerCombat>();
        if (combat == null)
        {
            Debug.Log("[WeaponPickup] LocalPlayer 에 PlayerCombat 미부착");
            return;
        }

        // 같은 무기 중복 등으로 픽업 거부 시 Destroy 안 함 (월드에 그대로 남음)
        // 네트워크 동기화 없음 - 다른 클라이언트의 화면에선 픽업이 계속 존재
        if (combat.TryPickupWeapon(_weaponPrefab))
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
