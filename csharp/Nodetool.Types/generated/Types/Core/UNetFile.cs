using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types.Core;

[MessagePackObject]
public class UNetFile
{
    [Key(0)]
    public object type { get; set; } = "comfy.unet_file";
    [Key(1)]
    public string name { get; set; } = "";
}
