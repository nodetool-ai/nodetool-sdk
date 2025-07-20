using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Core;

[MessagePackObject]
public class CountTokens
{
    [Key(0)]
    public string text { get; set; } = "";
    [Key(1)]
    public object encoding { get; set; } = "TiktokenEncoding.CL100K_BASE";

    public int Process()
    {
        // Implementation would be generated based on node logic
        return default(int);
    }
}
