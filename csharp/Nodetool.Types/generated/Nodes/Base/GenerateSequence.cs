using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.List;

[MessagePackObject]
public class GenerateSequence
{
    [Key(0)]
    public int start { get; set; } = 0;
    [Key(1)]
    public int stop { get; set; } = 0;
    [Key(2)]
    public int step { get; set; } = 1;

    public object Process()
    {
        // Implementation would be generated based on node logic
        return default(object);
    }
}
