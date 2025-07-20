using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Core;

[MessagePackObject]
public class Basename
{
    [Key(0)]
    public string path { get; set; } = "";
    [Key(1)]
    public bool remove_extension { get; set; } = false;

    public string Process()
    {
        // Implementation would be generated based on node logic
        return default(string);
    }
}
