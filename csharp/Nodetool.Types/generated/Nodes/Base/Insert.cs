using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class Insert
{
    [Key(0)]
    public object records { get; set; } = null;
    [Key(1)]
    public bool return_rows { get; set; } = true;
    [Key(2)]
    public string table_name { get; set; } = @"";

    public object Process()
    {
        return default(object);
    }
}
