using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class ExecuteSQL
{
    [Key(0)]
    public string database_name { get; set; } = @"memory.db";
    [Key(1)]
    public object parameters { get; set; } = new();
    [Key(2)]
    public string sql { get; set; } = @"SELECT * FROM flashcards";

    public object Process()
    {
        return default(object);
    }
}
