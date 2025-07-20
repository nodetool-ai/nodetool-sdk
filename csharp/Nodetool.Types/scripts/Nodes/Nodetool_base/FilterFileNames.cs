using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Nodetool_base;

[MessagePackObject]
public class FilterFileNames
{
    [Key(0)]
    public object filenames { get; set; } = new List<object>();
    [Key(1)]
    public string pattern { get; set; } = "*";
    [Key(2)]
    public bool case_sensitive { get; set; } = true;

    public object Process()
    {
        // Implementation would be generated based on node logic
        return default(object);
    }
}
