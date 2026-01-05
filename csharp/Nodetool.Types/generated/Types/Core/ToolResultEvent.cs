using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types.Core;

[MessagePackObject]
public class ToolResultEvent
{
    [Key(0)]
    public object error { get; set; } = null;
    [Key(1)]
    public string id { get; set; }
    [Key(2)]
    public object result { get; set; }
    [Key(3)]
    public object type { get; set; } = @"tool_result";
}
