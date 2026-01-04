using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class GetDatabasePath
{
    [Key(0)]
    public string database_name { get; set; } = @"memory.db";

    public string Process()
    {
        return default(string);
    }
}
