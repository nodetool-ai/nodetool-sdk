using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types;

[MessagePackObject]
public class Sampler
{
    [Key(0)]
    public object type { get; set; } = "comfy.sampler";
    [Key(1)]
    public object data { get; set; } = null;
}
