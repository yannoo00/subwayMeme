using UnityEngine;

// 바닥에 드랍된 변이 아이템
// 플레이어가 E키로 상호작용 시 변이 적용 후 소멸
public class DropItemPickup : MonoBehaviour, IInteractable
{
    private MutationDefinition _mutation;

    public void Init(MutationDefinition mutation)
    {
        _mutation = mutation;
    }

    public void Interact()
    {
        if (_mutation == null)
        {
            Debug.LogWarning("[DropItemPickup] MutationDefinition이 설정되지 않았습니다.");
            return;
        }

        MutationManager.Instance.ApplyMutation(_mutation);
        Destroy(gameObject);
    }

    public string GetHintText()
    {
        return $"E: {_mutation?.mutationName ?? "변이"} 획득";
    }
}
