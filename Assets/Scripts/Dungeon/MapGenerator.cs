using System.Collections.Generic;
using UnityEngine;


//node는 역을 의미함
//node를 잇는 path(edge)는 지하철이고 전투맵이라고 보면 됨
//이 map generator는 어떤 순서로 어떤 역을 배치할지 정하는 것
public enum NodeType
{
    shop,
    heal,
    trap,
    box,
}




public class MapGenerator : MonoBehaviour
{
    // === Inspector 변수 ===
    [Header("Map Size")]
    [SerializeField] private int _columns = 5;
    [SerializeField] private int _floors = 10;
    [SerializeField] private int _pathCount = 8;

    public StageMap GenerateMap(int seed)
    {
        Random.InitState(seed);


        //그리드 생성해서 노드들을 잇는 경로를 만들고
        StageNode[,] grid = CreateGrid();
        CreatePaths(grid);

        //맵 객체에 전달
        StageMap stageMap = new StageMap(_floors, _columns);
        for(int i=0; i<_floors; ++i)
        {
            for(int j=0; j<_columns; ++j)
            {
                stageMap.floors[i].Add(grid[i, j]);
            }
        }


        //맵 타입 할당
        AssignNodeTypes(stageMap);

        return stageMap;
    }

    /*
         규칙에 따라 맵 생성하는 메서드 시리즈
    */

    // 그리드 생성: _columns x _floors 크기의 StageNode 2D 배열 반환
    private StageNode[,] CreateGrid()
    {
        //그냥 정사각형 2차원 배열을 만드는 것
        StageNode[,] grid = new StageNode[_floors, _columns];

        for (int floor = 0; floor < _floors; floor++)
        {
            for (int col = 0; col < _columns; col++)
            {
                //배열 각 칸을 StageNode로 채워준다.
                grid[floor, col] = new StageNode(floor, col);
            }
        }

        return grid;
    }

    // 그리드 잇는 경로 생성: pathCount번 반복하여 1층부터 최상층 경로를 연결
    private void CreatePaths(StageNode[,] grid)
    {
        int firstStartCol = -1;

        //반복 한 번마다 하나의 경로 생성 
        for (int i = 0; i < _pathCount; ++i)
        {
            // 시작 열 선택: 처음 2개 경로는 반드시 다른 열에서 시작 (최소 2개의 진입점 보장)
            int startCol;
            if (i == 0)
            {
                startCol = Random.Range(0, _columns);
                firstStartCol = startCol;
            }
            else if (i == 1)
            {
                // 첫 번째 시작점과 다른 열이 나올 때까지 재시도
                do { startCol = Random.Range(0, _columns); }
                while (startCol == firstStartCol);
            }
            else
            {
                startCol = Random.Range(0, _columns);
            }

            int currentCol = startCol;

            for (int floor = 0; floor < _floors - 1; ++floor)
            {
                int nextCol = PickNextColumn(grid, floor, currentCol);

                StageNode from = grid[floor, currentCol];
                StageNode to   = grid[floor + 1, nextCol];

                // 이미 연결된 엣지는 중복 추가하지 않음
                if (!from.nextNodes.Contains(to))
                {
                    from.nextNodes.Add(to);
                    to.previousNodes.Add(from);
                }

                currentCol = nextCol;
            }
        }
    }

    // 현재 노드에서 다음 층의 열을 선택
    private int PickNextColumn(StageNode[,] grid, int floor, int currentCol)
    {
        // 인접 열 후보: currentCol-1, currentCol, currentCol+1 (범위 내로 제한)
        int minCol = Mathf.Max(0, currentCol - 1);
        int maxCol = Mathf.Min(_columns - 1, currentCol + 1);

        List<int> candidates = new();
        for (int col = minCol; col <= maxCol; col++)
        {
            candidates.Add(col);
        }

        // 유효한 후보가 없으면 현재 열 유지 (안전 폴백)
        if (candidates.Count == 0)
            return currentCol;

        return candidates[Random.Range(0, candidates.Count)];
    }



    //연결 안된 노드 삭제 
    //얘를 사용하지 않고 StageMap의 floor에 node들을 넣을 때 생각해도 됨 
    private void RemoveUnconnectedNodes(StageNode[,] grid)
    {
        for (int floor = 0; floor < _floors; floor++)
        {
            for (int col = 0; col < _columns; col++)
            {
                //어떤 path도 연결되지 않은 노드들은 여기서 삭제
                if ((grid[floor, col].previousNodes.Count == 0) && (grid[floor, col].nextNodes.Count == 0))
                {
                    //delete
                }
            }
        }
    }



    // 노드에 타입 할당: 층 규칙에 따라 고정 타입 배치, 나머지는 랜덤
    private void AssignNodeTypes(StageMap stageMap)
    {
        int totalTypes = System.Enum.GetValues(typeof(NodeType)).Length;

        for (int floor = 0; floor < stageMap.floors.Count; floor++)
        {
            foreach (StageNode node in stageMap.floors[floor])
            {
                //당장은 간단한 규칙만 적용.
                //첫번째 station이 shop이 되어서 반드시 템을 구매하든 얻든 하게 된다. 
                if (floor == 0)
                    node.type = NodeType.shop;
                else if (floor == 1)
                    node.type = NodeType.heal;
                else
                    node.type = (NodeType)Random.Range(0, totalTypes);
            }
        }
    }




    /*
        기타 유틸 함수들
    */
    private bool IsValidType(StageNode node, NodeType type)
    {
        //floor에 따라 정해진 타입이 아니면 false를 반환한다. 
        return false;
    }
}