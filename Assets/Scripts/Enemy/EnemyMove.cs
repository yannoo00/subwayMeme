using UnityEngine;

// 이동 방식 추상 클래스
// EnemyAI가 "어디로 갈지" 결정하면, 이 클래스가 "어떻게 이동할지" 처리
// 구체 클래스: NavMeshMove, FlyingMove 등
public abstract class EnemyMove : MonoBehaviour
{
    protected Enemy _enemy;


    protected virtual void Awake()
    {
        _enemy = GetComponent<Enemy>();
    }


    public abstract void MoveTo(Vector3 destination);
    public abstract void Stop();
}
