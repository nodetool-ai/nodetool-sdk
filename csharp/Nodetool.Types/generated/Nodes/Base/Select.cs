using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class Select
{
    [Key(0)]
    public Nodetool.Types.Core.RecordType columns { get; set; } = new Nodetool.Types.Core.RecordType();
    [Key(1)]
    public bool descending { get; set; } = false;
    [Key(2)]
    public object filters { get; set; } = null;
    [Key(3)]
    public int limit { get; set; } = 0;
    [Key(4)]
    public string order_by { get; set; } = @"";
    [Key(5)]
    public string table_name { get; set; } = @"";
    [Key(6)]
    public bool to_dataframe { get; set; } = false;

    public object Process()
    {
        return default(object);
    }
}
