using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class Query
{
    [Key(0)]
    public Nodetool.Types.Core.RecordType columns { get; set; } = new Nodetool.Types.Core.RecordType();
    [Key(1)]
    public string database_name { get; set; } = @"memory.db";
    [Key(2)]
    public int limit { get; set; } = 0;
    [Key(3)]
    public string order_by { get; set; } = @"";
    [Key(4)]
    public string table_name { get; set; } = @"flashcards";
    [Key(5)]
    public string where { get; set; } = @"";

    public object Process()
    {
        return default(object);
    }
}
