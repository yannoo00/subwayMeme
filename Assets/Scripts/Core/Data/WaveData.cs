using UnityEngine;

[System.Serializable]
public class EnemySpawnInfo
{
    public GameObject prefab;
    public int count;
}

// 웨이브 내 하나의 스폰 그룹
// 웨이브 시작 기준 delay초 후에 enemies를 스폰
[System.Serializable]
public class SpawnGroup
{
    public float delay;
    public EnemySpawnInfo[] enemies;
}




[CreateAssetMenu(fileName = "WaveData", menuName = "Scriptable Objects/WaveData")]
public class WaveData : ScriptableObject
{
    [Header("웨이브 정보")]
    public string waveName;

    [Header("타이밍")]
    // 이 웨이브의 진행 시간 (StageManager 타이머가 이 값을 사용)
    public float duration = 60f;

    [Header("스폰 그룹")]
    // 각 그룹은 delay 시점에 독립적으로 스폰됨
    // 예: delay 0 -> 좀비x2, delay 15 -> 런너x1, delay 40 -> 헤비x1 + 좀비x3
    public SpawnGroup[] spawnGroups;
}
