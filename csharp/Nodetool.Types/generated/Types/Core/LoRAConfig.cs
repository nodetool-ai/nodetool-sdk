using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types.Core;

[MessagePackObject]
public class LoRAConfig
{
    [Key(0)]
    public object type { get; set; } = "comfy.lora_config";
    [Key(1)]
    public Nodetool.Types.Core.LORAFile lora { get; set; } = new Nodetool.Types.Core.LORAFile();
    [Key(2)]
    public double strength { get; set; } = 1.0;
}

