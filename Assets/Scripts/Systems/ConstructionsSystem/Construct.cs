using System.Collections.Generic;

namespace ConstructionSystem
{
    public class Construct
    {
        List<Block> Blocks { get; } = new List<Block>();
        List<Connection> Connections { get; } = new List<Connection>();
    }
}