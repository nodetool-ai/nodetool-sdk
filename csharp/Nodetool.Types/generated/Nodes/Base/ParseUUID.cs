using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class ParseUUID
{
    [Key(0)]
    public string uuid_string { get; set; } = @"";

    public object Process()
    {
        return default(object);
    }
}
