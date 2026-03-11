using UnityEngine.UIElements;

public class ShopInteractable : StationInteractable
{
    // 상점 UI 요소
    private Button _closeButton;

    public override void Interact()
    {
        OpenUI();
    }

    public override string GetHintText() => "E: 상점 열기";

    protected override void BindUI()
    {
        _closeButton = _root.Q<Button>("close-button");

        if (_closeButton != null)
            _closeButton.clicked += CloseUI;

        // TODO: 판매 아이템 목록 표시
        // TODO: 골드 잔액 표시
        // TODO: 구매 버튼 이벤트 연결
    }
}
