using System.Collections.Generic;
using GameProto;
using UnityEngine;

// 서버에서 받은 MutationId 로 MutationDefinition 을 찾기 위한 ScriptableObject
// Resources/MutationDatabase.asset 에 두면 정적 접근자로 자동 로드됨
[CreateAssetMenu(fileName = "MutationDatabase", menuName = "Game/Mutation Database")]
public class MutationDatabase : ScriptableObject
{
    private static MutationDatabase _instance;

    public static MutationDatabase Instance
    {
        get
        {
            if (_instance != null) return _instance;

            _instance = Resources.Load<MutationDatabase>("MutationDatabase");
            if (_instance == null)
            {
                Debug.LogError("[MutationDatabase] Resources/MutationDatabase.asset 을 찾지 못했습니다.");
                return null;
            }
            _instance.BuildLookup();
            return _instance;
        }
    }

    [SerializeField] private MutationDefinition[] _mutations;

    private Dictionary<MutationId, MutationDefinition> _lookup;

    public MutationDefinition Get(MutationId id)
    {
        if (_lookup == null) BuildLookup();
        _lookup.TryGetValue(id, out var def);
        return def;
    }

    private void BuildLookup()
    {
        _lookup = new Dictionary<MutationId, MutationDefinition>();
        if (_mutations == null) return;

        foreach (var m in _mutations)
        {
            if (m == null) continue;
            if (_lookup.ContainsKey(m.mutationId))
            {
                Debug.LogError($"[MutationDatabase] 중복 ID: {m.mutationId} ({m.mutationName})");
                continue;
            }
            _lookup[m.mutationId] = m;
        }
    }
}
