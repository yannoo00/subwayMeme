using System;
using UnityEngine;

// 전투 관련 이벤트
public static class CombatEvents
{
    public static event Action<GameObject> OnEnemySpawned;
    public static event Action<GameObject> OnEnemyDied;
    public static event Action<GameObject> OnBossSpawned;

    public static void EnemySpawned(GameObject enemy) => OnEnemySpawned?.Invoke(enemy);
    public static void EnemyDied(GameObject enemy) => OnEnemyDied?.Invoke(enemy);
    public static void BossSpawned(GameObject boss) => OnBossSpawned?.Invoke(boss);
}
