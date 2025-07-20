using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types;

[MessagePackObject]
public class ToolName
{
    [Key(0)]
    public object type { get; set; } = "tool_name";
    [Key(1)]
    public string name { get; set; } = "";
}
