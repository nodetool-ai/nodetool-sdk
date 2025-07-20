using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Core;

[MessagePackObject]
public class GetCloseMatches
{
    [Key(0)]
    public string word { get; set; } = "";
    [Key(1)]
    public object possibilities { get; set; } = new List<object>();
    [Key(2)]
    public int n { get; set; } = 3;
    [Key(3)]
    public double cutoff { get; set; } = 0.6;

    public object Process()
    {
        // Implementation would be generated based on node logic
        return default(object);
    }
}
