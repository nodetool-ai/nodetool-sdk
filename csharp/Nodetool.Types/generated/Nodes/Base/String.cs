using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class String
{
    [Key(0)]
    public string value { get; set; } = @"";

    public string Process()
    {
        return default(string);
    }
}
