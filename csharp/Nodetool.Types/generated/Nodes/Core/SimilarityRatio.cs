using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Core;

[MessagePackObject]
public class SimilarityRatio
{
    [Key(0)]
    public string a { get; set; } = "";
    [Key(1)]
    public string b { get; set; } = "";

    public double Process()
    {
        // Implementation would be generated based on node logic
        return default(double);
    }
}
