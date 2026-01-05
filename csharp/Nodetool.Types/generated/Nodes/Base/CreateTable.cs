using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class CreateTable
{
    [Key(0)]
    public bool add_primary_key { get; set; } = true;
    [Key(1)]
    public Nodetool.Types.Core.RecordType columns { get; set; } = new Nodetool.Types.Core.RecordType();
    [Key(2)]
    public string database_name { get; set; } = @"memory.db";
    [Key(3)]
    public bool if_not_exists { get; set; } = true;
    [Key(4)]
    public string table_name { get; set; } = @"flashcards";

    [MessagePackObject]
    public class CreateTableOutput
    {
        [Key(0)]
        public Nodetool.Types.Core.RecordType columns { get; set; }
        [Key(1)]
        public string database_name { get; set; }
        [Key(2)]
        public string table_name { get; set; }
    }

    public CreateTableOutput Process()
    {
        return new CreateTableOutput();
    }
}
