using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class CountTokens
{
    [Key(0)]
    public object encoding { get; set; } = @"cl100k_base";
    [Key(1)]
    public string text { get; set; } = @"";

    public int Process()
    {
        return default(int);
    }
}
