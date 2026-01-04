using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class ExecuteLua
{
    [Key(0)]
    public string code { get; set; } = @"";
    [Key(1)]
    public object executable { get; set; }
    [Key(2)]
    public object execution_mode { get; set; }
    [Key(3)]
    public string stdin { get; set; } = @"";
    [Key(4)]
    public int timeout_seconds { get; set; } = 10;

    [MessagePackObject]
    public class ExecuteLuaOutput
    {
        [Key(0)]
        public string stderr { get; set; }
        [Key(1)]
        public string stdout { get; set; }
    }

    public ExecuteLuaOutput Process()
    {
        return new ExecuteLuaOutput();
    }
}
