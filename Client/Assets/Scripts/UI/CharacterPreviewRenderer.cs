using UnityEngine;

// 캐릭터 선택 모달의 3D 미리보기 렌더러
// 씬에 배치된 전용 Camera가 SpawnPoint 앞의 캐릭터 모델을 RenderTexture에 담음
// MainMenuUIDocument가 PreviewTexture를 .char-preview VisualElement 배경에 연결
public class CharacterPreviewRenderer : MonoBehaviour
{
    [SerializeField] private Camera    _previewCamera;
    [SerializeField] private Transform _spawnPoint;

    [Header("Render Texture")]
    [SerializeField] private int _rtWidth  = 512;
    [SerializeField] private int _rtHeight = 768;

    public RenderTexture PreviewTexture { get; private set; }

    private GameObject _currentModel;

    private void Awake()
    {
        PreviewTexture = new RenderTexture(_rtWidth, _rtHeight, 24);
        _previewCamera.targetTexture = PreviewTexture;
    }

    private void OnDestroy()
    {
        if (PreviewTexture != null)
        {
            _previewCamera.targetTexture = null;
            PreviewTexture.Release();
            Destroy(PreviewTexture);
        }
    }

    // 캐릭터 카드 클릭 시 호출 - 이전 모델 제거 후 새 모델 스폰
    public void Preview(CharacterDefinition def)
    {
        ClearPreview();
        if (def?.modelPrefab == null) return;

        _currentModel = Instantiate(def.modelPrefab, _spawnPoint);
        _currentModel.transform.localPosition = Vector3.zero;
        _currentModel.transform.localRotation = Quaternion.identity;
    }

    // 모달 닫을 때 호출 - 불필요한 모델 제거
    public void ClearPreview()
    {
        if (_currentModel == null) return;
        Destroy(_currentModel);
        _currentModel = null;
    }
}
