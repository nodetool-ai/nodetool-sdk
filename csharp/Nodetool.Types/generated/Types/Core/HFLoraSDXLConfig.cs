using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types.Core;

[MessagePackObject]
public class HFLoraSDXLConfig
{
    [Key(0)]
    public object type { get; set; } = "hf.lora_sdxl_config";
    [Key(1)]
    public Nodetool.Types.Core.HFLoraSDXL lora { get; set; } = new Nodetool.Types.Core.HFLoraSDXL();
    [Key(2)]
    public double strength { get; set; } = 0.5;
}

