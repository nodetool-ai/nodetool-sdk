using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Nodetool_huggingface;

[MessagePackObject]
public class LoRASelector
{
    [Key(0)]
    public Nodetool.Types.HFLoraSD lora1 { get; set; } = new Nodetool.Types.HFLoraSD();
    [Key(1)]
    public double strength1 { get; set; } = 1.0;
    [Key(2)]
    public Nodetool.Types.HFLoraSD lora2 { get; set; } = new Nodetool.Types.HFLoraSD();
    [Key(3)]
    public double strength2 { get; set; } = 1.0;
    [Key(4)]
    public Nodetool.Types.HFLoraSD lora3 { get; set; } = new Nodetool.Types.HFLoraSD();
    [Key(5)]
    public double strength3 { get; set; } = 1.0;
    [Key(6)]
    public Nodetool.Types.HFLoraSD lora4 { get; set; } = new Nodetool.Types.HFLoraSD();
    [Key(7)]
    public double strength4 { get; set; } = 1.0;
    [Key(8)]
    public Nodetool.Types.HFLoraSD lora5 { get; set; } = new Nodetool.Types.HFLoraSD();
    [Key(9)]
    public double strength5 { get; set; } = 1.0;

    public object Process()
    {
        // Implementation would be generated based on node logic
        return default(object);
    }
}
