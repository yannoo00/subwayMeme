using UnityEngine;

// 특정 characterId의 modelPrefab을 ModelHolder 자식으로 인스턴시에이션하고,
// 모델 안의 WeaponSocket을 PlayerCombat에 주입한다 (PlayerCombat이 붙어있을 때만).
//
// 로컬 플레이어: _autoBindOnAwake = true 로 두면 Awake 시점에
// GameManager.SelectedCharacterId 로 자동 바인딩.
// 원격 플레이어: _autoBindOnAwake = false 로 두고, NetworkPlayer.Init 에서
// Bind(characterId) 를 명시적으로 호출. (다른 사람이 선택한 캐릭터로 바인딩)
public class PlayerCharacterBinder : MonoBehaviour
{
    [Header("References")]
    // 모델이 인스턴시에이션될 부모 Transform (Player.prefab의 ModelHolder)
    [SerializeField] private Transform _modelHolder;

    [Header("Binding")]
    // true: Awake에서 자동으로 GameManager.SelectedCharacterId 로 바인딩 (로컬 플레이어 기본)
    // false: 외부에서 Bind(characterId) 를 명시 호출해야 함 (원격 플레이어)
    [SerializeField] private bool _autoBindOnAwake = true;

    // 모델 안에서 무기 소켓을 찾을 때 사용하는 이름. 컨벤션 일치 필수
    private const string WEAPON_SOCKET_NAME = "WeaponSocket";

    // Bind 완료 후 발견된 WeaponSocket Transform.
    // 로컬은 PlayerCombat 이 SetWeaponHolder 로 받지만, 원격 (NetworkPlayer) 은 이 프로퍼티로 직접 참조.
    public Transform WeaponSocket { get; private set; }


    private void Awake()
    {
        // 원격 플레이어 경로에서는 NetworkPlayer.Init 이 Bind 를 호출할 때까지 대기
        if (!_autoBindOnAwake) return;

        var gm = GameManager.Instance;
        if (gm == null)
        {
            Debug.LogWarning($"[PlayerCharacterBinder] GameManager.Instance == null. " +
                             $"현재 씬: {gameObject.scene.name}. " +
                             $"Player.prefab이 GameManager 생성 전에 Awake되는 구조일 가능성.");
            return;
        }

        Bind(gm.SelectedCharacterId);
    }

    // 외부에서 명시적으로 특정 characterId로 모델을 바인딩
    // 원격 플레이어는 NetworkPlayer.Init 이 GamePlayerInfo.CharacterId 로 이 메서드 호출
    public bool Bind(int characterId)
    {
        if (_modelHolder == null)
        {
            Debug.LogError("[PlayerCharacterBinder] _modelHolder 미할당 - Inspector에서 설정 필요");
            return false;
        }

        var gm = GameManager.Instance;
        if (gm == null)
        {
            Debug.LogWarning("[PlayerCharacterBinder] Bind 호출 시점에 GameManager.Instance == null");
            return false;
        }

        var def = gm.GetCharacterDefinition(characterId);
        if (def == null)
        {
            int allCharsCount = gm.AllCharacters != null ? gm.AllCharacters.Length : -1;
            Debug.LogWarning($"[PlayerCharacterBinder] CharacterDefinition == null. " +
                             $"characterId={characterId}, AllCharacters.Length={allCharsCount}. " +
                             $"원인: (1) GameManager._characters 배열 비어있음, " +
                             $"(2) characterId={characterId}인 SO가 배열에 없음.");
            return false;
        }

        if (def.modelPrefab == null)
        {
            Debug.LogWarning($"[PlayerCharacterBinder] '{def.displayName}' (id={def.characterId}) 의 modelPrefab 미할당");
            return false;
        }

        // 방어적 정리: 에디터에서 미리 박혀 있는 자식이 있어도 깔끔하게 시작
        // (정상 흐름에서는 ModelHolder가 비어 있어야 함)
        for (int i = _modelHolder.childCount - 1; i >= 0; i--)
            Destroy(_modelHolder.GetChild(i).gameObject);

        // localPosition/Rotation을 0으로 두려면 부모 지정 + identity 변환
        var model = Instantiate(def.modelPrefab, _modelHolder);
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.identity;

        // 모델 내부에서 weapon socket 찾기 - 못 찾으면 경고만 (원격 플레이어는 무기 인터랙션 없을 수도)
        var socket = FindDescendantByName(model.transform, WEAPON_SOCKET_NAME);
        if (socket == null)
        {
            Debug.LogWarning($"[PlayerCharacterBinder] '{def.displayName}' 모델에 '{WEAPON_SOCKET_NAME}' 자식이 없음");
        }
        WeaponSocket = socket;

        // PlayerCombat 은 로컬 플레이어에만 붙음. 있을 때만 무기 소켓 주입
        if (TryGetComponent<PlayerCombat>(out var combat) && socket != null)
        {
            combat.SetWeaponHolder(socket);
        }

        return true;
    }


    // 깊이 우선으로 자손 중에서 이름이 일치하는 첫 Transform 반환
    // Transform.Find는 직접 자식만 보므로 본 트리 안쪽에 있는 socket을 못 찾음
    private static Transform FindDescendantByName(Transform parent, string targetName)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            var child = parent.GetChild(i);
            if (child.name == targetName) return child;

            var found = FindDescendantByName(child, targetName);
            if (found != null) return found;
        }
        return null;
    }
}
