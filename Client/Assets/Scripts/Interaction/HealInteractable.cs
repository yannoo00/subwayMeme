using UnityEngine.UIElements;

public class HealInteractable : StationInteractable
{
    private Button _healButton;
    private Button _closeButton;

    public override void Interact()
    {
        OpenUI();
    }

    public override string GetHintText() => "E: 회복 스테이션 사용";

    protected override void BindUI()
    {
        _healButton  = _root.Q<Button>("heal-button");
        _closeButton = _root.Q<Button>("close-button");

        if (_healButton != null)
            _healButton.clicked += OnHealClicked;

        if (_closeButton != null)
            _closeButton.clicked += CloseUI;
    }

    private void OnHealClicked()
    {
        // TODO: PlayerStats에 회복량 적용
        // TODO: 1회 사용 후 버튼 비활성화
        CloseUI();
    }
}
