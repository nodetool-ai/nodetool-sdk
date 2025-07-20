using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types.Core;

[MessagePackObject]
public class Sigmas
{
    [Key(0)]
    public object type { get; set; } = "comfy.sigmas";
    [Key(1)]
    public object data { get; set; } = null;
}
