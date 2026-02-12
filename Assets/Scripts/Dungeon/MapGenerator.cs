

using UnityEngine;
using UnityEngine.Rendering.Universal;

public enum NodeType
{
    shop,
    heal,
    trap,
    box,
}

class MapGenerator: MonoBehaviour
{
    public StageMap GenerateMap(int seed)
    {
        return new StageMap();
    }

    // 규칙에 따라 맵 생성 

}