using UnityEngine;

// DontDestroyOnLoad가 필요하지만 전용 싱글톤 스크립트가 없는 오브젝트에 부착
// 예: EventSystem
public class PersistentObject : MonoBehaviour
{
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }
}
