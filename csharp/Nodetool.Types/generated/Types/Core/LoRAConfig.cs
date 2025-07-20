using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types.Core;

[MessagePackObject]
public class LoRAConfig
{
    [Key(0)]
    public object type { get; set; } = "comfy.lora_config";
    [Key(1)]
    public Nodetool.Types.LORAFile lora { get; set; } = new Nodetool.Types.LORAFile();
    [Key(2)]
    public double strength { get; set; } = 1.0;
}
