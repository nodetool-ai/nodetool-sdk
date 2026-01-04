using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types.Core;

[MessagePackObject]
public class HFLoraSDConfig
{
    [Key(0)]
    public Nodetool.Types.Core.HFLoraSD lora { get; set; } = new Nodetool.Types.Core.HFLoraSD();
    [Key(1)]
    public double strength { get; set; } = 0.5;
    [Key(2)]
    public object type { get; set; } = @"hf.lora_sd_config";
}
