

using System.Collections.Generic;

public class StageMap
{
    public List<List<StageNode>> floors; // 층별 노드
    public StageNode bossNode; // 보스 노드는 따로 관리 
    public StageNode currentNode; // 현재 역 

}