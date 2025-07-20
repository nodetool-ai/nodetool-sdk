using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types.Core;

[MessagePackObject]
public class Latent
{
    [Key(0)]
    public object type { get; set; } = "comfy.latent";
    [Key(1)]
    public object data { get; set; } = null;
}
