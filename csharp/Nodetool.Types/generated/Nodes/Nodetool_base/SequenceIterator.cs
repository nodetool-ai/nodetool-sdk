using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Nodetool_base;

[MessagePackObject]
public class SequenceIterator
{
    [Key(0)]
    public int start { get; set; } = 0;
    [Key(1)]
    public int stop { get; set; } = 0;
    [Key(2)]
    public int step { get; set; } = 1;

    public void Process()
    {
        // Implementation would be generated based on node logic
    }
}
