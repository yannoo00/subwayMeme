using UnityEngine;

// 액티브 스킬 추상 클래스
// 변이로 획득, 슬롯 3/4에 배치
// WeaponBase와 달리 "전환" 없이 3키/4키로 즉시 발동
public abstract class SkillBase : MonoBehaviour
{
    [Header("Skill Data")]
    [SerializeField] protected SkillData _skillData;

    public SkillData SkillData => _skillData;

    private float _lastUseTime = -999f;

    public bool CanActivate => Time.time >= _lastUseTime + _skillData.cooldown;
    public float CooldownRemaining => Mathf.Max(0f, (_lastUseTime + _skillData.cooldown) - Time.time);

    public bool TryActivate()
    {
        if (!CanActivate) return false;

        _lastUseTime = Time.time;
        PerformSkill();
        return true;
    }

    // 각 스킬 구체 클래스에서 구현
    protected abstract void PerformSkill();
}
