using System.Collections.Generic;



public class StageMap
{
    public List<List<StageNode>> floors; // 층별 노드
    public StageNode bossNode; // 보스 노드는 따로 관리 
    public StageNode currentNode; // 현재 역

    public int maxColumns; 


    // floor x columns로 생성한 다음, 연결 안되는 노드들은 삭제되는 것에 유의
    public StageMap(int floors, int columns)
    {
        this.floors = new List<List<StageNode>>();
        this.maxColumns = columns;

        for (int i = 0; i < floors; i++)
        {
           this.floors.Add(new List<StageNode>()); 
        }
    }

    public int GetIndex(StageNode node)
    {
        return node.floor * maxColumns + node.column;
    }

    public StageNode GetNodeByIndex(int idx)
    {

        int f = idx/maxColumns;
        int c = idx%maxColumns;

        foreach( var node in floors[f])
        {
            if(node.column == c)
                return node;
        }
        
        return null;
    }
}