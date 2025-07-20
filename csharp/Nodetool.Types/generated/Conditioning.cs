using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types;

[MessagePackObject]
public class Conditioning
{
    [Key(0)]
    public object type { get; set; } = "comfy.conditioning";
    [Key(1)]
    public object data { get; set; } = null;
}
