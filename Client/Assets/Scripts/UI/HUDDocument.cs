using UnityEngine;
using UnityEngine.UIElements;


// UI Toolkit 기반 인게임 HUD
// HP, 타이머, 무기 슬롯, 탄약, 재장전 진행률 표시
// DontDestroyOnLoad로 씬 전환 후에도 유지
public class HUDDocument : MonoBehaviour
{
    // === Inspector 변수 ===

    [Header("Style")]
    [SerializeField] private StyleSheet _styleSheet;


    // === Private 변수 ===

    private const string HiddenClass = "hud-panel--hidden";

    private UIDocument _document;

    private VisualElement _hpPanel;
    private VisualElement _hpBarFill;
    private Label _hpLabel;

    private VisualElement _timerPanel;
    private Label _timerTitle;
    private Label _timerLabel;

    private VisualElement _generatorBarFill;
    private Label _generatorLabel;

    // 무기 슬롯 - 인덱스 = 슬롯 번호
    private VisualElement   _weaponPanel;
    private VisualElement[] _weaponSlots = new VisualElement[2];
    private Label[]         _weaponSlotNames = new Label[2];

    // 탄약 패널
    private VisualElement _ammoPanel;
    private Label         _ammoStatus;
    private VisualElement _reloadBarBg;
    private VisualElement _reloadBarFill;

    // 크로스헤어
    private VisualElement _crosshair;

    // 게임 결과 패널 (클리어/오버)
    private VisualElement _resultPanel;
    private Label         _resultTitle;

    // 관전 패널
    private VisualElement _spectatorPanel;
    private Label         _spectatorTarget;

    // 재장전 진행률 추적용
    private bool  _isReloading;
    private float _reloadStartTime;
    private float _reloadDuration;


    // === Unity 생명주기 ===

    private void Awake()
    {
        _document = GetComponent<UIDocument>();
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        BindUI();

        PlayerEvents.OnHealthChanged      += OnHealthChanged;
        PlayerEvents.OnWeaponSlotChanged  += OnWeaponSlotChanged;
        PlayerEvents.OnActiveSlotChanged  += OnActiveSlotChanged;
        PlayerEvents.OnAmmoChanged        += OnAmmoChanged;
        PlayerEvents.OnReloadStarted      += OnReloadStarted;
        PlayerEvents.OnReloadFinished     += OnReloadFinished;
        PlayerEvents.OnAimStateChanged    += OnAimStateChanged;
        StageEvents.OnSubwayStarted       += OnSubwayStarted;
        StageEvents.OnStationArrived      += OnStationArrived;
        StageEvents.OnSurvivalStarted     += OnSurvivalStarted;
        StageEvents.OnTimerTick           += OnTimerTick;
        GameEvents.OnGeneratorDamaged     += OnGeneratorDamaged;
        GameEvents.OnGameOver             += OnGameOver;
        GameEvents.OnGameClear            += OnGameClear;
        PlayerEvents.OnSpectateTargetChanged += OnSpectateTargetChanged;
    }

    private void OnDisable()
    {
        PlayerEvents.OnHealthChanged      -= OnHealthChanged;
        PlayerEvents.OnWeaponSlotChanged  -= OnWeaponSlotChanged;
        PlayerEvents.OnActiveSlotChanged  -= OnActiveSlotChanged;
        PlayerEvents.OnAmmoChanged        -= OnAmmoChanged;
        PlayerEvents.OnReloadStarted      -= OnReloadStarted;
        PlayerEvents.OnReloadFinished     -= OnReloadFinished;
        PlayerEvents.OnAimStateChanged    -= OnAimStateChanged;
        StageEvents.OnSubwayStarted       -= OnSubwayStarted;
        StageEvents.OnStationArrived      -= OnStationArrived;
        StageEvents.OnSurvivalStarted     -= OnSurvivalStarted;
        StageEvents.OnTimerTick           -= OnTimerTick;
        GameEvents.OnGeneratorDamaged     -= OnGeneratorDamaged;
        GameEvents.OnGameOver             -= OnGameOver;
        GameEvents.OnGameClear            -= OnGameClear;
        PlayerEvents.OnSpectateTargetChanged -= OnSpectateTargetChanged;
    }

    private void Update()
    {
        // 재장전 중일 때만 진행률 바 너비 갱신. 다른 시간에는 즉시 return으로 부담 거의 없음
        if (!_isReloading) return;
        if (_reloadDuration <= 0f) return;

        float elapsed = Time.time - _reloadStartTime;
        float ratio = Mathf.Clamp01(elapsed / _reloadDuration);
        _reloadBarFill.style.width = Length.Percent(ratio * 100f);
    }


    // === UI 바인딩 ===

    private void BindUI()
    {
        var root = _document.rootVisualElement;

        if (_styleSheet != null)
            root.styleSheets.Add(_styleSheet);

        _hpPanel    = root.Q<VisualElement>("hp-panel");
        _hpBarFill  = root.Q<VisualElement>("hp-bar-fill");
        _hpLabel    = root.Q<Label>("hp-label");

        _timerPanel = root.Q<VisualElement>("timer-panel");
        _timerTitle = root.Q<Label>("timer-title");
        _timerLabel = root.Q<Label>("timer-label");

        _generatorBarFill = root.Q<VisualElement>("generator-bar-fill");
        _generatorLabel   = root.Q<Label>("generator-label");

        _weaponPanel        = root.Q<VisualElement>("weapon-panel");
        _weaponSlots[0]     = root.Q<VisualElement>("weapon-slot-0");
        _weaponSlots[1]     = root.Q<VisualElement>("weapon-slot-1");
        _weaponSlotNames[0] = root.Q<Label>("weapon-slot-0-name");
        _weaponSlotNames[1] = root.Q<Label>("weapon-slot-1-name");

        _ammoPanel     = root.Q<VisualElement>("ammo-panel");
        _ammoStatus    = root.Q<Label>("ammo-status");
        _reloadBarBg   = root.Q<VisualElement>("reload-bar-bg");
        _reloadBarFill = root.Q<VisualElement>("reload-bar-fill");

        _crosshair = root.Q<VisualElement>("crosshair");
        SetHidden(_crosshair, true);

        _resultPanel = root.Q<VisualElement>("game-result-panel");
        _resultTitle = root.Q<Label>("result-title");
        SetHidden(_resultPanel, true);

        _spectatorPanel  = root.Q<VisualElement>("spectator-panel");
        _spectatorTarget = root.Q<Label>("spectator-target");
        SetHidden(_spectatorPanel, true);
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
        _timerPanel.RemoveFromClassList(HiddenClass);
    }

    private void OnStationArrived(StageNode node)
    {
        _timerTitle.text = "출발까지";
        _timerPanel.RemoveFromClassList(HiddenClass);
    }

    private void OnSurvivalStarted()
    {
        _timerTitle.text = "남은 시간";
        _timerPanel.RemoveFromClassList(HiddenClass);
    }

    private void OnTimerTick(float remaining, float total)
    {
        int minutes = Mathf.FloorToInt(remaining / 60f);
        int seconds = Mathf.FloorToInt(remaining % 60f);
        _timerLabel.text = $"{minutes:00}:{seconds:00}";
    }

    private void OnGeneratorDamaged(int current, int max)
    {
        if (_generatorBarFill == null || _generatorLabel == null) return;

        float ratio = max > 0 ? (float)current / max : 0f;
        _generatorBarFill.style.width = Length.Percent(ratio * 100f);
        _generatorLabel.text = $"발전기 {current} / {max}";
    }


    private void OnGameClear()
    {
        ShowResult("GAME CLEAR", "result-title--clear");
    }

    private void OnGameOver()
    {
        ShowResult("GAME OVER", "result-title--over");
    }

    // 결과 패널 표시: 타이틀 텍스트/컬러 클래스 세팅 후 패널 노출
    private void ShowResult(string title, string variantClass)
    {
        if (_resultPanel == null || _resultTitle == null) return;

        _resultTitle.text = title;
        _resultTitle.RemoveFromClassList("result-title--clear");
        _resultTitle.RemoveFromClassList("result-title--over");
        _resultTitle.AddToClassList(variantClass);

        SetHidden(_resultPanel, false);
    }

    // 로컬 플레이어 사망 후 관전 모드 진입/대상 전환 시 호출
    // 관전 표시자를 띄우고, 더 이상 의미 없는 내 전투 패널들을 숨김
    private void OnSpectateTargetChanged(NetworkPlayer target)
    {
        if (target == null) return;

        _spectatorTarget.text = target.PlayerName;
        SetHidden(_spectatorPanel, false);

        SetHidden(_hpPanel, true);
        SetHidden(_weaponPanel, true);
        SetHidden(_ammoPanel, true);
        SetHidden(_crosshair, true);
    }


    // === 무기 슬롯 갱신 ===

    // 슬롯 내용 변경(픽업/드랍/스왑) 시 호출. data == null이면 빈 슬롯
    private void OnWeaponSlotChanged(int slotIndex, WeaponData data)
    {
        if (slotIndex < 0 || slotIndex >= _weaponSlots.Length) return;
        if (_weaponSlots[slotIndex] == null) return;

        bool isEmpty = data == null;
        _weaponSlotNames[slotIndex].text = isEmpty ? "Empty" : data.weaponName;

        // EnableInClassList(name, true): 클래스 추가, false: 제거
        _weaponSlots[slotIndex].EnableInClassList("weapon-slot--empty", isEmpty);
    }

    // 활성 슬롯 변경(1/2 키 또는 새 무기 픽업) 시 호출
    // 슬롯 강조 + 활성화된 무기 타입에 따라 탄약 패널 가시성 결정
    private void OnActiveSlotChanged(int activeIndex, WeaponData activeData)
    {
        for (int i = 0; i < _weaponSlots.Length; i++)
        {
            if (_weaponSlots[i] == null) continue;
            _weaponSlots[i].EnableInClassList("weapon-slot--active", i == activeIndex);
        }

        // Hitscan 무기일 때만 탄약 패널 표시. 근접/빈 슬롯이면 숨김
        bool isHitscan = activeData is HitscanWeaponData;
        SetHidden(_ammoPanel, !isHitscan);

        // 무기 전환으로 패널이 숨겨지면 진행 중이던 재장전 표시도 정리
        // (실제 무기의 재장전은 백그라운드에서 진행. Equip 시 다시 ReloadStarted 발생)
        if (!isHitscan && _isReloading)
        {
            _isReloading = false;
            SetHidden(_reloadBarBg, true);
        }
    }


    // === 탄약 / 재장전 갱신 ===

    private void OnAmmoChanged(int current, int max)
    {
        // 라벨은 항상 갱신. 패널 가시성은 OnActiveSlotChanged가 별도 관리
        // 단 재장전 중에는 "RELOADING" 텍스트 유지 (ReloadFinished가 먼저 와서 _isReloading이 false인 상태)
        if (_isReloading) return;

        _ammoStatus.text = $"{current} / {max}";
    }

    private void OnReloadStarted(float duration)
    {
        _isReloading     = true;
        _reloadStartTime = Time.time;
        _reloadDuration  = duration;

        _ammoStatus.text = "RELOADING";
        _reloadBarFill.style.width = Length.Percent(0f);
        SetHidden(_reloadBarBg, false);
    }

    private void OnReloadFinished()
    {
        _isReloading = false;
        SetHidden(_reloadBarBg, true);
        // 라벨 갱신은 뒤따라오는 OnAmmoChanged가 처리
    }


    private void OnAimStateChanged(bool isAiming, int weaponTypeID)
    {
        SetHidden(_crosshair, !isAiming);
    }


    // === 헬퍼 ===

    private static void SetHidden(VisualElement element, bool hidden)
    {
        if (element == null) return;
        element.EnableInClassList(HiddenClass, hidden);
    }
}
