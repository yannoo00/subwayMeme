using UnityEngine;
using UnityEngine.UIElements;

// 역에서 소환되는 상호작용 오브젝트의 공통 추상 클래스
// UI Toolkit 기반으로 UI를 열고 닫는 공통 흐름을 제공
// IInteractable을 함께 구현해서 PlayerInteractor가 E키로 동일하게 처리 가능
public abstract class StationInteractable : MonoBehaviour, IInteractable
{
    protected UIDocument _document;
    protected VisualElement _root;

    protected virtual void Awake()
    {
        _document = GetComponent<UIDocument>();
    }


    // UI 열기 공통 흐름: 루트 표시 후 각 클래스가 요소 바인딩
    protected virtual void OpenUI()
    {
        _root = _document.rootVisualElement;
        _root.style.display = DisplayStyle.Flex;
        BindUI();
    }

    // UI 닫기
    protected virtual void CloseUI()
    {
        _root.style.display = DisplayStyle.None;
    }

    // 각 클래스가 자신의 UI 요소를 쿼리하고 이벤트를 연결
    protected abstract void BindUI();

    // IInteractable 구현 강제
    public abstract void Interact();
    public abstract string GetHintText();
}
