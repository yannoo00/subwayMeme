using UnityEngine.UIElements;

// 보물 상자: 상호작용 시 아이템 획득 UI 표시
public class BoxInteractable : StationInteractable
{
    private Button _openButton;
    private Label _itemLabel;

    private bool _isOpened = false;

    public override void Interact()
    {
        if (_isOpened) return;
        OpenUI();
    }

    public override string GetHintText() => _isOpened ? "" : "E: 상자 열기";

    protected override void BindUI()
    {
        _openButton = _root.Q<Button>("open-button");
        _itemLabel  = _root.Q<Label>("item-label");

        if (_openButton != null)
            _openButton.clicked += OnOpenClicked;
    }

    private void OnOpenClicked()
    {
        _isOpened = true;

        // TODO: 랜덤 아이템 결정 및 플레이어 인벤토리에 추가
        // TODO: _itemLabel에 획득 아이템 이름 표시
        CloseUI();
    }
}
