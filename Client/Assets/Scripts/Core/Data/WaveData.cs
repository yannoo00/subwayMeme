using UnityEngine;

[System.Serializable]
public class EnemySpawnInfo
{
    public GameObject prefab;
    public int count;
}

// 웨이브 내 하나의 스폰 그룹
// delay초 후 첫 스폰. interval > 0이면 주기적으로 반복 스폰
[System.Serializable]
public class SpawnGroup
{
    public float delay;

    // 0이면 1회 스폰 후 종료 (기존 동작)
    // 0보다 크면 interval 간격으로 반복 스폰
    public float interval;

    // -1이면 무한 반복, 1 이상이면 해당 횟수만큼만 반복 (첫 스폰 포함)
    public int repeatCount = -1;

    public EnemySpawnInfo[] enemies;
}


[CreateAssetMenu(fileName = "WaveData", menuName = "Scriptable Objects/WaveData")]
public class WaveData : ScriptableObject
{
    [Header("웨이브 정보")]
    public string waveName;

    [Header("타이밍")]
    // 서바이벌 모드에서는 StageManager가 별도 타이머를 쓰므로 이 값은 무시됨
    public float duration = 60f;

    [Header("스폰 그룹")]
    // 각 그룹은 delay 시점에 독립적으로 스폰 시작
    // 예: delay=0 interval=60 repeat=-1 -> 0초부터 60초마다 무한 반복
    //     delay=120 interval=120 repeat=-1 -> 2분부터 2분마다 Elite 스폰
    public SpawnGroup[] spawnGroups;
}
