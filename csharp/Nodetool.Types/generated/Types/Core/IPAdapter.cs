using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types.Core;

[MessagePackObject]
public class IPAdapter
{
    [Key(0)]
    public object model { get; set; } = null;
    [Key(1)]
    public string name { get; set; } = @"";
    [Key(2)]
    public object type { get; set; } = @"comfy.ip_adapter";
}
