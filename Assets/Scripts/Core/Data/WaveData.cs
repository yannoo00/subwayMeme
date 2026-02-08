using UnityEngine;

[System.Serializable]
public class EnemySpawnInfo
{
    public GameObject prefab;
    public int count;
}

[CreateAssetMenu(fileName = "WaveData", menuName = "Scriptable Objects/WaveData")]
public class WaveData : ScriptableObject
{
    [Header("웨이브 정보")]
    public string waveName;

    [Header("스폰할 적 목록")]
    public EnemySpawnInfo[] enemies;


    // 이 웨이브의 총 적 수
    public int TotalEnemyCount
    {
        get
        {
            int total = 0;
            foreach (var info in enemies)
            {
                total += info.count;
            }
            return total;
        }
    }
}
