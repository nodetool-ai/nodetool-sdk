using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class BrowserNavigation
{
    [Key(0)]
    public object action { get; set; } = @"goto";
    [Key(1)]
    public string attribute { get; set; } = @"";
    [Key(2)]
    public object extract_type { get; set; } = @"text";
    [Key(3)]
    public string selector { get; set; } = @"";
    [Key(4)]
    public int timeout { get; set; } = 30000;
    [Key(5)]
    public string url { get; set; } = @"";
    [Key(6)]
    public string wait_for { get; set; } = @"";

    public object Process()
    {
        return default(object);
    }
}
