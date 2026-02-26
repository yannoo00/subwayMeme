using System;
using UnityEngine;
using UnityEngine.UI;


public enum NodeState 
{ 
    Normal, 
    Available, 
    Current, 
    Visited 
}


// 노선도 위의 역 하나를 나타내는 UI 컴포넌트
// 프리팹에 Button + Image 컴포넌트 필요
public class NodeVisual : MonoBehaviour
{
    // Inspector에서 NodeState 순서(Normal/Available/Current/Visited)에 맞춰 스프라이트 할당
    [SerializeField] private Sprite[] _stateSprites;

    private Image _image;
    private Button _button;

    private void Awake()
    {
        _image = GetComponent<Image>();
        _button = GetComponent<Button>();
    }

    public void Setup(StageNode node, Action<StageNode> onSelected)
    {
        _button.onClick.AddListener(() => onSelected(node));
        SetState(NodeState.Normal);
    }

    public void SetState(NodeState state)
    {
        int index = (int)state;
        if (index < _stateSprites.Length && _stateSprites[index] != null)
            _image.sprite = _stateSprites[index];

        // Available 상태일 때만 클릭 가능
        _button.interactable = (state == NodeState.Available);
    }
}
