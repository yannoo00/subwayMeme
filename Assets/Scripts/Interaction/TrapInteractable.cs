using UnityEngine.UIElements;

// 미스테리 자판기: 상호작용 시 랜덤 효과 (긍정/부정 모두 가능)
public class TrapInteractable : StationInteractable
{
    private Button _useButton;
    private Button _closeButton;
    private Label _descriptionLabel;

    public override void Interact()
    {
        OpenUI();
    }

    public override string GetHintText() => "E: 자판기 사용";

    protected override void BindUI()
    {
        _useButton          = _root.Q<Button>("use-button");
        _closeButton        = _root.Q<Button>("close-button");
        _descriptionLabel   = _root.Q<Label>("description-label");

        if (_useButton != null)
            _useButton.clicked += OnUseClicked;

        if (_closeButton != null)
            _closeButton.clicked += CloseUI;

        // TODO: 결과를 알 수 없다는 힌트 문구 표시
    }

    private void OnUseClicked()
    {
        // TODO: 랜덤 효과 풀에서 하나 선택 (회복, 데미지, 버프, 디버프 등)
        // TODO: 선택된 효과를 _descriptionLabel에 표시
        // TODO: 효과 적용 후 use-button 비활성화 (1회 사용)
        CloseUI();
    }
}
