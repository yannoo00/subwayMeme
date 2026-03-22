using System.Collections.Generic;

public class StageNode
{
    public int floor;
    public int column;
    public NodeType type;
    public StageData stageData;

    public List<StageNode> nextNodes; 
    public List<StageNode> previousNodes; 

    public bool visited = false;


    public StageNode(int floor, int column)
    {
        this.floor      = floor;
        this.column     = column; 
        nextNodes       = new List<StageNode>();
        previousNodes   = new List<StageNode>();
    }
}
