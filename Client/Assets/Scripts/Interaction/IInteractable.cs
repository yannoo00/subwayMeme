// 상호작용 가능한 오브젝트의 공통 인터페이스
// VendingMachine, Shop 등이 이 인터페이스를 구현
public interface IInteractable
{
    // E키 눌렸을 때 실행할 동작
    void Interact();

    // 상호작용 가능 범위 안에 있을 때 표시할 안내 문구 (예: "E: 탑승")
    string GetHintText();
}


