using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types.Core;

[MessagePackObject]
public class ToolCallEvent
{
    [Key(0)]
    public Dictionary<string, object> args { get; set; }
    [Key(1)]
    public string id { get; set; }
    [Key(2)]
    public string name { get; set; }
    [Key(3)]
    public object type { get; set; } = @"tool_call";
}
