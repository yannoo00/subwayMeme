using UnityEngine;
using UnityEngine.UI;

// 메인 메뉴 패널에 붙이는 컴포넌트
// Inspector에서 패널 오브젝트와 시작 버튼 연결
public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private GameObject _panel;
    [SerializeField] private Button _startButton;

    private void Start()
    {
        _startButton.onClick.AddListener(OnStartClicked);
    }

    private void OnStartClicked()
    {
        Debug.Log("onStartClicked! ");
        _panel.SetActive(false);
        GameManager.Instance.StartGame();
    }
}
