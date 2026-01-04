using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class ExecuteCommand
{
    [Key(0)]
    public string command { get; set; } = @"";
    [Key(1)]
    public object execution_mode { get; set; }
    [Key(2)]
    public object image { get; set; }
    [Key(3)]
    public string stdin { get; set; } = @"";

    [MessagePackObject]
    public class ExecuteCommandOutput
    {
        [Key(0)]
        public string stderr { get; set; }
        [Key(1)]
        public string stdout { get; set; }
    }

    public ExecuteCommandOutput Process()
    {
        return new ExecuteCommandOutput();
    }
}
