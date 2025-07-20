using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types;

[MessagePackObject]
public class Noise
{
    [Key(0)]
    public object type { get; set; } = "comfy.noise";
    [Key(1)]
    public object data { get; set; } = null;
}
