using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Nodetool_base;

[MessagePackObject]
public class FindAllRegex
{
    [Key(0)]
    public string text { get; set; } = "";
    [Key(1)]
    public string regex { get; set; } = "";
    [Key(2)]
    public bool dotall { get; set; } = false;
    [Key(3)]
    public bool ignorecase { get; set; } = false;
    [Key(4)]
    public bool multiline { get; set; } = false;

    public object Process()
    {
        // Implementation would be generated based on node logic
        return default(object);
    }
}
