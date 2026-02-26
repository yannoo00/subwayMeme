using UnityEngine;
using UnityEngine.UI;

// 노선도 위의 경로 선 하나를 나타내는 UI 컴포넌트
// 프리팹에 Image 컴포넌트 필요 (가느다란 선 스프라이트 사용)
public class PathVisual : MonoBehaviour
{
    [SerializeField] private Color _activeColor = Color.white;
    [SerializeField] private Color _visitedColor = Color.gray;

    private Image _image;

    private void Awake()
    {
        _image = GetComponent<Image>();
        _image.color = _activeColor;
    }

    public void SetVisited(bool visited)
    {
        _image.color = visited ? _visitedColor : _activeColor;
    }
}
