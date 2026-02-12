using System.Collections.Generic;

public class StageNode
{
    public int floor;
    public int column;
    public NodeType type;

    public List<StageNode> nextNodes; 
    public List<StageNode> previousNodes; 

    public bool visited = false;
}