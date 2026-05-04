using UnityEngine;

// 패시브 변이 동작의 추상 클래스
// MutationManager가 플레이어 자식으로 Instantiate
// 예: HP 재생, 가시 반사 등 매 프레임 또는 이벤트 기반 동작
public abstract class PassiveBase : MonoBehaviour
{
    protected virtual void Awake()
    {
        OnPassiveStart();
    }

    // 컴포넌트 부착 시 초기화 (버프 등록, 코루틴 시작 등)
    protected abstract void OnPassiveStart();
}
