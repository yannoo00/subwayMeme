using UnityEngine;

// 캐릭터 한 명의 메타데이터를 정의하는 ScriptableObject
// Create > Game > Character Definition 으로 생성
//
// 강화 트리 정책 (Scenario A):
// 모든 캐릭터가 동일한 UpgradeDefinition 목록을 공유하고, 캐릭터별로 레벨만 따로 저장된다
// (CharacterSaveData.upgradeLevels). 캐릭터마다 다른 트리가 필요해지면 이 SO에
// UpgradeDefinition[] 필드를 추가하고 UpgradeManager 가 그걸 참조하도록 변경한다.
[CreateAssetMenu(fileName = "CharacterDefinition", menuName = "Game/Character Definition")]
public class CharacterDefinition : ScriptableObject
{
    [Header("기본 정보")]
    public int characterId;
    public string displayName;
    [TextArea] public string description;

    [Header("표현")]
    public Sprite icon;

    [Header("기본 스탯")]
    // PlayerStats의 _baseMaxHealth로 사용
    public int baseMaxHealth = 100;
    // PlayerStats.AttackBonus 에 가산되는 캐릭터 베이스 공격력 보너스 (0.1 = +10%)
    public float baseAttackPower = 0f;
    // PlayerController._moveSpeed 로 사용
    public float baseMoveSpeed = 5f;
    // PlayerController._dodgeCooldown 으로 사용 (초)
    public float baseDodgeCooldown = 1.5f;

    [Header("외형/장비")]
    // PlayerCharacterBinder가 Player의 ModelHolder 자식으로 인스턴시에이션
    // 모델 프리팹 안에 "WeaponSocket"이라는 이름의 자식 Transform이 있어야 함 (손 위치)
    public GameObject modelPrefab;
}
