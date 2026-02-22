using UnityEngine;
using UnityEngine.InputSystem;

// 플레이어의 상호작용 처리 컴포넌트
// 매 프레임 범위 내 IInteractable을 탐색하고, E키 입력 시 Interact() 호출
public class PlayerInteractor : MonoBehaviour
{
    [Header("Interaction Settings")]
    [SerializeField] private float _interactRange = 2f;

    private IInteractable _currentInteractable;

    // 현재 상호작용 가능한 오브젝트 (힌트 UI 표시용으로 외부 접근 가능)
    public IInteractable CurrentInteractable => _currentInteractable;


    private void Update()
    {
        UpdateCurrentInteractable();

        if (Keyboard.current == null) return;

        // wasPressedThisFrame: 이번 프레임에 처음 눌렸을 때만 실행
        // isPressed는 누르는 동안 매 프레임 true 이므로, 연속 상호작용 방지를 위해 wasPressedThisFrame 사용
        if (Keyboard.current.eKey.wasPressedThisFrame && _currentInteractable != null)
        {
            _currentInteractable.Interact();
        }
    }


    // 범위 내에서 가장 가까운 IInteractable 탐색
    private void UpdateCurrentInteractable()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, _interactRange);

        IInteractable closest = null;
        float closestDist = float.MaxValue;

        foreach (Collider hit in hits)
        {
            IInteractable interactable = hit.GetComponent<IInteractable>();
            if (interactable == null) continue;

            float dist = Vector3.Distance(transform.position, hit.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = interactable;
            }
        }

        _currentInteractable = closest;
    }


    // 에디터에서 상호작용 범위를 노란 와이어 구로 시각화(디버그용)
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _interactRange);
    }
}
