using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class Upsert
{
    [Key(0)]
    public string on_conflict { get; set; } = null;
    [Key(1)]
    public object records { get; set; } = null;
    [Key(2)]
    public bool return_rows { get; set; } = true;
    [Key(3)]
    public string table_name { get; set; } = @"";

    public object Process()
    {
        return default(object);
    }
}
