using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Text;

[MessagePackObject]
public class RegexMatch
{
    [Key(0)]
    public string text { get; set; } = "";
    [Key(1)]
    public string pattern { get; set; } = "";
    [Key(2)]
    public object group { get; set; } = null;

    public object Process()
    {
        // Implementation would be generated based on node logic
        return default(object);
    }
}
