using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types.Core;

[MessagePackObject]
public class Guider
{
    [Key(0)]
    public object type { get; set; } = "comfy.guider";
    [Key(1)]
    public object data { get; set; } = null;
}
