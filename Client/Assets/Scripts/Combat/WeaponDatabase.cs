using System.Collections.Generic;
using UnityEngine;

// WeaponType -> 무기 프리팹 lookup 싱글톤.
// 원격 플레이어가 S_Aiming 수신 시 weapon_type_id 만 알고 있어서, 어떤 프리팹을 WeaponSocket 에
// 인스턴시에이션할지 결정하려면 이 lookup 이 필요.
// 로컬 픽업/장착 플로우 (WeaponPickup -> PlayerCombat.EquipToSlot) 는 손대지 않고 그대로 둠 -
// 원격 표시 전용 게이트웨이로만 동작.
//
// MutationManager 의 ScriptableObject 배열 -> Dictionary lookup 패턴을 그대로 미러링.
public class WeaponDatabase : MonoBehaviour
{
    public static WeaponDatabase Instance { get; private set; }

    [Header("등록된 무기 프리팹")]
    // 각 프리팹은 WeaponBase + weaponData 가 박혀있어야 함 (Inspector 검증)
    [SerializeField] private GameObject[] _weaponPrefabs;

    private Dictionary<WeaponType, GameObject> _lookup;


    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        BuildLookup();
    }


    private void BuildLookup()
    {
        _lookup = new Dictionary<WeaponType, GameObject>();
        if (_weaponPrefabs == null) return;

        foreach (var prefab in _weaponPrefabs)
        {
            if (prefab == null) continue;

            var wb = prefab.GetComponent<WeaponBase>();
            if (wb == null || wb.weaponData == null)
            {
                Debug.LogError($"[WeaponDatabase] '{prefab.name}' 에 WeaponBase 또는 weaponData 가 없습니다.");
                continue;
            }

            var type = wb.weaponData.weaponType;
            if (_lookup.ContainsKey(type))
            {
                Debug.LogError($"[WeaponDatabase] 중복 WeaponType: {type} (기존={_lookup[type].name}, 신규={prefab.name})");
                continue;
            }

            _lookup[type] = prefab;
        }
    }


    // 원격 플레이어가 호출. 등록되지 않은 타입이면 null
    public GameObject GetPrefab(WeaponType type)
        => _lookup != null && _lookup.TryGetValue(type, out var prefab) ? prefab : null;
}
