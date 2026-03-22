using UnityEngine;
using UnityEngine.UIElements;


// UI Toolkit 기반 메인 메뉴
// UIDocument 컴포넌트가 있는 GameObject에 함께 부착
// 기존 MainMenuUI.cs(UGUI)를 대체
public class MainMenuUIDocument : MonoBehaviour
{
    // === Inspector 변수 ===

    [Header("Style")]
    [SerializeField] private StyleSheet _styleSheet;


    // === Private 변수 ===

    private UIDocument _document;
    private VisualElement _panel;
    private Button _startButton;


    // === Unity 생명주기 ===

    private void Awake()
    {
        _document = GetComponent<UIDocument>();
    }

    private void OnEnable()
    {
        BindUI();
    }

    private void OnDisable()
    {
        if (_startButton != null)
            _startButton.clicked -= OnStartClicked;
    }


    // === UI 바인딩 ===

    private void BindUI()
    {
        var root = _document.rootVisualElement;

        if (_styleSheet != null)
            root.styleSheets.Add(_styleSheet);

        _panel       = root.Q<VisualElement>("main-menu-panel");
        _startButton = root.Q<Button>("start-button");

        _startButton.clicked += OnStartClicked;
    }


    // === 이벤트 처리 ===

    private void OnStartClicked()
    {
        Debug.Log("onStartClicked!");
        _panel.style.display = DisplayStyle.None;
        GameManager.Instance.StartGame();
    }
}
