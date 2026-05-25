using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// 씬 비동기 로드를 담당하는 싱글톤
// DontDestroyOnLoad로 씬 전환 후에도 유지
public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance { get; private set; }

    private const string SCENE_STATION = "Station";
    private const string SCENE_SUBWAY  = "Subway";


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }


    // Station 씬으로 전환. 로드 완료 후 onLoaded 콜백 실행
    public void LoadStation(Action onLoaded = null)
    {
        StartCoroutine(LoadSceneAsync(SCENE_STATION, onLoaded));
    }


    // Subway 씬으로 전환. 로드 완료 후 onLoaded 콜백 실행
    public void LoadSubway(Action onLoaded = null)
    {
        StartCoroutine(LoadSceneAsync(SCENE_SUBWAY, onLoaded));
    }


    // Single 모드로 씬 로드 (이전 씬 교체)
    // DontDestroyOnLoad 오브젝트들은 씬 전환 후에도 자동 유지됨
    private IEnumerator LoadSceneAsync(string sceneName, Action onLoaded)
    {
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        yield return op;

        // 새 씬의 PlayerSpawnPoint로 플레이어 이동
        RepositionPlayer();

        onLoaded?.Invoke();
    }


    // "PlayerSpawnPoint" 태그 오브젝트 위치로 로컬 플레이어를 텔레포트
    // 태그가 없으면 이동하지 않음 (폴백)
    // 원격 플레이어는 PlayerRegistry.OnSceneLoaded 에서 별도로 처리됨
    private void RepositionPlayer()
    {
        GameObject spawnPoint = GameObject.FindGameObjectWithTag("PlayerSpawnPoint");
        if (spawnPoint == null) return;

        GameObject player = PlayerRegistry.Instance != null ? PlayerRegistry.Instance.LocalPlayer : null;
        if (player == null) return;

        player.transform.SetPositionAndRotation(
            spawnPoint.transform.position,
            spawnPoint.transform.rotation
        );
    }
}
