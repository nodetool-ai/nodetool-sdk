using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Core;

[MessagePackObject]
public class ExtractHeaders
{
    [Key(0)]
    public string markdown { get; set; } = "";
    [Key(1)]
    public int max_level { get; set; } = 6;

    public object Process()
    {
        // Implementation would be generated based on node logic
        return default(object);
    }
}
