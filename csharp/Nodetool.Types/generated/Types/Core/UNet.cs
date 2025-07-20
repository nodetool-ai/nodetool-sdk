using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types.Core;

[MessagePackObject]
public class UNet
{
    [Key(0)]
    public object type { get; set; } = "comfy.unet";
    [Key(1)]
    public string name { get; set; } = "";
    [Key(2)]
    public object model { get; set; } = null;
}
