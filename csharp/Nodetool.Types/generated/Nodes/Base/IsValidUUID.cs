using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class IsValidUUID
{
    [Key(0)]
    public string uuid_string { get; set; } = @"";

    public bool Process()
    {
        return default(bool);
    }
}
