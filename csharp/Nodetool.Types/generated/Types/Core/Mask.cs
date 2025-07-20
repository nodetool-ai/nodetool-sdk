using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types.Core;

[MessagePackObject]
public class Mask
{
    [Key(0)]
    public object type { get; set; } = "comfy.mask";
    [Key(1)]
    public object data { get; set; } = null;
}
