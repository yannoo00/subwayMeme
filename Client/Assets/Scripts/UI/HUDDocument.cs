using UnityEngine;
using UnityEngine.UIElements;


// UI Toolkit 기반 인게임 HUD
// HP, 타이머 표시
// DontDestroyOnLoad로 씬 전환 후에도 유지
public class HUDDocument : MonoBehaviour
{
    // === Inspector 변수 ===

    [Header("Style")]
    [SerializeField] private StyleSheet _styleSheet;


    // === Private 변수 ===

    private UIDocument _document;

    private VisualElement _hpBarFill;
    private Label _hpLabel;

    private VisualElement _timerPanel;
    private Label _timerTitle;
    private Label _timerLabel;


    // === Unity 생명주기 ===

    private void Awake()
    {
        _document = GetComponent<UIDocument>();
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        BindUI();

        PlayerEvents.OnHealthChanged += OnHealthChanged;
        StageEvents.OnSubwayStarted  += OnSubwayStarted;
        StageEvents.OnStationArrived += OnStationArrived;
        StageEvents.OnTimerTick      += OnTimerTick;
    }

    private void OnDisable()
    {
        PlayerEvents.OnHealthChanged -= OnHealthChanged;
        StageEvents.OnSubwayStarted  -= OnSubwayStarted;
        StageEvents.OnStationArrived -= OnStationArrived;
        StageEvents.OnTimerTick      -= OnTimerTick;
    }


    // === UI 바인딩 ===

    private void BindUI()
    {
        var root = _document.rootVisualElement;

        if (_styleSheet != null)
            root.styleSheets.Add(_styleSheet);

        _hpBarFill  = root.Q<VisualElement>("hp-bar-fill");
        _hpLabel    = root.Q<Label>("hp-label");

        _timerPanel = root.Q<VisualElement>("timer-panel");
        _timerTitle = root.Q<Label>("timer-title");
        _timerLabel = root.Q<Label>("timer-label");
    }


    // === 이벤트 핸들러 ===

    private void OnHealthChanged(int current, int max)
    {
        float ratio = max > 0 ? (float)current / max : 0f;

        // HP 바 너비를 퍼센트로 설정
        _hpBarFill.style.width = Length.Percent(ratio * 100f);
        _hpLabel.text = $"{current} / {max}";
    }

    private void OnSubwayStarted(StageNode node)
    {
        _timerTitle.text = "다음 역까지";
        _timerPanel.RemoveFromClassList("hud-panel--hidden");
    }

    private void OnStationArrived(StageNode node)
    {
        _timerTitle.text = "출발까지";
        _timerPanel.RemoveFromClassList("hud-panel--hidden");
    }

    private void OnTimerTick(float remaining, float total)
    {
        int minutes = Mathf.FloorToInt(remaining / 60f);
        int seconds = Mathf.FloorToInt(remaining % 60f);
        _timerLabel.text = $"{minutes:00}:{seconds:00}";
    }
}
