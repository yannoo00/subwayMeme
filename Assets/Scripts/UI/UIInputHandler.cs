using UnityEngine;
using UnityEngine.InputSystem;

// UI 관련 키 입력을 감지하여 이벤트를 발행하는 컴포넌트
// 키 설정은 Inspector에서 변경 가능
public class UIInputHandler : MonoBehaviour
{
    [Header("Keys")]
    [SerializeField] private Key _mapKey       = Key.M;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }
    // [SerializeField] private Key _inventoryKey = Key.I;  // 인벤토리 구현 시 활성화

    private void Update()
    {
        if (Keyboard.current[_mapKey].wasPressedThisFrame)
            StageEvents.MapOpenRequested(MapOpenReason.ViewOnly, null);

        // if (Keyboard.current[_inventoryKey].wasPressedThisFrame)
        //     UIEvents.InventoryToggleRequested();
    }
}
