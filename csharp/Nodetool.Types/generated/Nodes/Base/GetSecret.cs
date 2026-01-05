using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class GetSecret
{
    [Key(0)]
    public string default_ { get; set; } = null;
    [Key(1)]
    public string name { get; set; } = @"";

    public string Process()
    {
        return default(string);
    }
}
