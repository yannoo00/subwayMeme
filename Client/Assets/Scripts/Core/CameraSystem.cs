using UnityEngine;

// CameraSystem 오브젝트를 모든 씬에서 유지하는 DDOL 싱글톤
// Freelook/Aim 가상카메라와 CinemachineBrain을 포함한 채로 씬 전환 시 살아남음
public class CameraSystem : MonoBehaviour
{
    public static CameraSystem Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
}
