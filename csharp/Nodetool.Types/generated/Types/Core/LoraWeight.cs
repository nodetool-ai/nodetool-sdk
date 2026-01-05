using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types.Core;

[MessagePackObject]
public class LoraWeight
{
    [Key(0)]
    public double scale { get; set; } = 1.0;
    [Key(1)]
    public object type { get; set; } = @"lora_weight";
    [Key(2)]
    public string url { get; set; } = @"";
}
