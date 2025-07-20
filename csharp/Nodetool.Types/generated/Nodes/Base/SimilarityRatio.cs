using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class SimilarityRatio
{
    [Key(0)]
    public string a { get; set; } = "";
    [Key(1)]
    public string b { get; set; } = "";

    public double Process()
    {
        return default(double);
    }
}
