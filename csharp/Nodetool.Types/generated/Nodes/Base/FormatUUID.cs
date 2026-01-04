using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class FormatUUID
{
    [Key(0)]
    public object format { get; set; } = @"standard";
    [Key(1)]
    public string uuid_string { get; set; } = @"";

    public string Process()
    {
        return default(string);
    }
}
