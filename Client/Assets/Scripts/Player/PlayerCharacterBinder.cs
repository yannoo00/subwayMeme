using UnityEngine;

// SelectedCharacter의 modelPrefab을 ModelHolder 자식으로 인스턴시에이션하고,
// 모델 안의 WeaponSocket을 PlayerCombat에 주입한다.
//
// Awake 단계에서 실행되므로 다른 컴포넌트의 Start보다 먼저 모델 + 소켓 준비 완료.
// PlayerCombat.Start()의 GetComponentInChildren<Animator>()가 새 모델의 Animator를 자동 발견.
[RequireComponent(typeof(PlayerCombat))]
public class PlayerCharacterBinder : MonoBehaviour
{
    [Header("References")]
    // 모델이 인스턴시에이션될 부모 Transform (Player.prefab의 ModelHolder)
    [SerializeField] private Transform _modelHolder;

    // 모델 안에서 무기 소켓을 찾을 때 사용하는 이름. 컨벤션 일치 필수
    private const string WEAPON_SOCKET_NAME = "WeaponSocket";


    private void Awake()
    {
        if (_modelHolder == null)
        {
            Debug.LogError("[PlayerCharacterBinder] _modelHolder 미할당 - Inspector에서 설정 필요");
            return;
        }

        // 진단: 왜 null인지 알 수 있도록 단계별로 검사
        var gm = GameManager.Instance;
        if (gm == null)
        {
            Debug.LogWarning($"[PlayerCharacterBinder] GameManager.Instance == null. " +
                             $"현재 씬: {gameObject.scene.name}. " +
                             $"Player.prefab이 GameManager 생성 전에 Awake되는 구조일 가능성.");
            return;
        }

        var allChars = gm.AllCharacters;
        int allCharsCount = allChars != null ? allChars.Length : -1;
        int selectedId    = gm.SelectedCharacterId;

        var def = gm.SelectedCharacter;
        if (def == null)
        {
            Debug.LogWarning($"[PlayerCharacterBinder] SelectedCharacter == null. " +
                             $"SelectedCharacterId={selectedId}, AllCharacters.Length={allCharsCount}. " +
                             $"원인: (1) GameManager._characters 배열 비어있음, " +
                             $"(2) characterId={selectedId}인 SO가 배열에 없음.");

            // 배열에 어떤 characterId들이 있는지 추가 출력
            if (allChars != null)
            {
                for (int i = 0; i < allChars.Length; i++)
                {
                    var c = allChars[i];
                    Debug.LogWarning($"  [{i}] {(c == null ? "null" : $"id={c.characterId}, name={c.displayName}")}");
                }
            }
            return;
        }
        if (def.modelPrefab == null)
        {
            Debug.LogWarning($"[PlayerCharacterBinder] '{def.displayName}' (id={def.characterId}) 의 modelPrefab 미할당");
            return;
        }

        // 방어적 정리: 에디터에서 미리 박혀 있는 자식이 있어도 깔끔하게 시작
        // (정상 흐름에서는 ModelHolder가 비어 있어야 함)
        for (int i = _modelHolder.childCount - 1; i >= 0; i--)
            Destroy(_modelHolder.GetChild(i).gameObject);

        // localPosition/Rotation을 0으로 두려면 부모 지정 + identity 변환
        var model = Instantiate(def.modelPrefab, _modelHolder);
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.identity;

        // 모델 내부에서 weapon socket 찾아 PlayerCombat에 주입
        // 모델마다 손 위치가 다르므로 본 트리 안 어딘가에 "WeaponSocket" 자식이 있어야 함
        var socket = FindDescendantByName(model.transform, WEAPON_SOCKET_NAME);
        if (socket == null)
        {
            Debug.LogError($"[PlayerCharacterBinder] '{def.displayName}' 모델에 '{WEAPON_SOCKET_NAME}' 자식이 없음");
            return;
        }

        GetComponent<PlayerCombat>().SetWeaponHolder(socket);
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
