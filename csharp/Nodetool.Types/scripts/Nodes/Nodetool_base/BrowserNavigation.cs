using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Nodetool_base;

[MessagePackObject]
public class BrowserNavigation
{
    [Key(0)]
    public string url { get; set; } = "";
    [Key(1)]
    public object action { get; set; } = "Action.GOTO";
    [Key(2)]
    public string selector { get; set; } = "";
    [Key(3)]
    public int timeout { get; set; } = 30000;
    [Key(4)]
    public string wait_for { get; set; } = "";
    [Key(5)]
    public object extract_type { get; set; } = "ExtractType.TEXT";
    [Key(6)]
    public string attribute { get; set; } = "";

    public object Process()
    {
        // Implementation would be generated based on node logic
        return default(object);
    }
}
