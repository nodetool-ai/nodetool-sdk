using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class ShowNotification
{
    [Key(0)]
    public string message { get; set; } = @"";
    [Key(1)]
    public int timeout { get; set; } = 10;
    [Key(2)]
    public string title { get; set; } = @"";

    public object Process()
    {
        return default(object);
    }
}
