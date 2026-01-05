using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class Delete
{
    [Key(0)]
    public object filters { get; set; } = null;
    [Key(1)]
    public string table_name { get; set; } = @"";

    public object Process()
    {
        return default(object);
    }
}
