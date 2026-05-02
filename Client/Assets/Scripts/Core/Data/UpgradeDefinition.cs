using UnityEngine;

// 업그레이드 종류 인덱스 - SaveData.upgradeLevels 배열 인덱스와 일치해야 함
public enum UpgradeType
{
    MaxHp        = 0,
    AttackDamage = 1,
    DodgeCooldown = 2,
    Count        = 3
}

// 강화 항목 하나를 정의하는 ScriptableObject
// Create > Game > Upgrade Definition 으로 생성
[CreateAssetMenu(fileName = "UpgradeDefinition", menuName = "Game/Upgrade Definition")]
public class UpgradeDefinition : ScriptableObject
{
    [Header("기본 정보")]
    public UpgradeType type;
    public string displayName;
    [TextArea] public string descriptionFormat; // "{0}" 위치에 총 효과량 표시

    [Header("강화 수치")]
    public int maxLevel = 5;
    public int[]   costPerLevel;   // 각 레벨 업그레이드 비용 (길이 == maxLevel)
    public float[] valuePerLevel;  // 각 레벨 추가 효과량 (길이 == maxLevel)

    // currentLevel 레벨에서 업그레이드 시 필요한 비용. 최대 레벨이면 -1 반환
    public int GetCost(int currentLevel)
    {
        if (currentLevel < 0 || currentLevel >= maxLevel) return -1;
        if (costPerLevel == null || currentLevel >= costPerLevel.Length) return -1;
        return costPerLevel[currentLevel];
    }

    // 0 ~ level-1 까지 valuePerLevel 합산 반환
    public float GetTotalValue(int level)
    {
        float total = 0f;
        int cap = Mathf.Min(level, valuePerLevel != null ? valuePerLevel.Length : 0);
        for (int i = 0; i < cap; i++)
            total += valuePerLevel[i];
        return total;
    }
}
