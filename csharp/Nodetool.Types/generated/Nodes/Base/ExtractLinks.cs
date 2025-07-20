using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Lib;

[MessagePackObject]
public class ExtractLinks
{
    [Key(0)]
    public string markdown { get; set; } = "";
    [Key(1)]
    public bool include_titles { get; set; } = true;

    public object Process()
    {
        // Implementation would be generated based on node logic
        return default(object);
    }
}
