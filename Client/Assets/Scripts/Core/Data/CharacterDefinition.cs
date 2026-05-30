using UnityEngine;

// 캐릭터 한 명의 메타데이터를 정의하는 ScriptableObject
// Create > Game > Character Definition 으로 생성
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
    // PlayerStats.AttackBonus 에 가산되는 캐릭터 베이스 공격력 보너스. 퍼센트 단위 (10 = +10%)
    // 실제 데미지 적용 시: damage * (1 + baseAttackPower / 100)
    public float baseAttackPower = 0f;
    // PlayerController._moveSpeed 로 사용
    public float baseMoveSpeed = 5f;


    [Header("스태미나")]
    // PlayerStats._baseMaxStamina 로 사용
    public float baseMaxStamina = 100f;
    // dash 1초당 소비량
    public float baseStaminaDrain = 25f;
    // 1초당 회복량
    public float baseStaminaRegen = 15f;

    [Header("외형/장비")]
    // PlayerCharacterBinder가 Player의 ModelHolder 자식으로 인스턴시에이션
    // 모델 프리팹 안에 "WeaponSocket"이라는 이름의 자식 Transform이 있어야 함 (손 위치)
    public GameObject modelPrefab;
}
