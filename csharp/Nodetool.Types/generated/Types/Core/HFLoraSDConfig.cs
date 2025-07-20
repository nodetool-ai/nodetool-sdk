using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types.Core;

[MessagePackObject]
public class HFLoraSDConfig
{
    [Key(0)]
    public object type { get; set; } = "hf.lora_sd_config";
    [Key(1)]
    public Nodetool.Types.HFLoraSD lora { get; set; } = new Nodetool.Types.HFLoraSD();
    [Key(2)]
    public double strength { get; set; } = 0.5;
}
