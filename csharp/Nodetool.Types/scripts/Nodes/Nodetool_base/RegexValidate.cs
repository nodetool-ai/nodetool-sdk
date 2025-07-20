using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Nodetool_base;

[MessagePackObject]
public class RegexValidate
{
    [Key(0)]
    public string text { get; set; } = "";
    [Key(1)]
    public string pattern { get; set; } = "";

    public bool Process()
    {
        // Implementation would be generated based on node logic
        return default(bool);
    }
}
