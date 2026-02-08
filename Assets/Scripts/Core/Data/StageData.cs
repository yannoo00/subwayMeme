using UnityEngine;

[CreateAssetMenu(fileName = "StageData", menuName = "Scriptable Objects/StageData")]
public class StageData : ScriptableObject
{
    [Header("스테이지 정보")]
    public string stageName;

    [Header("사용 가능한 웨이브 목록")]
    [Tooltip("이 스테이지에서 랜덤하게 선택될 웨이브들")]
    public WaveData[] availableWaves;

    
    // 랜덤하게 웨이브 선택
    public WaveData GetRandomWave()
    {
        if (availableWaves == null || availableWaves.Length == 0)
        {
            Debug.LogWarning($"[StageData] {stageName}에 사용 가능한 웨이브가 없습니다!");
            return null;
        }

        int randomIndex = Random.Range(0, availableWaves.Length);
        return availableWaves[randomIndex];
    }
}
