using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types;

[MessagePackObject]
public class LoraWeight
{
    [Key(0)]
    public object type { get; set; } = "lora_weight";
    [Key(1)]
    public string url { get; set; } = "";
    [Key(2)]
    public double scale { get; set; } = 1.0;
}
