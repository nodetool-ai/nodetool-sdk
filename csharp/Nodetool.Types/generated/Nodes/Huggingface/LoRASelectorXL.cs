using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Huggingface;

[MessagePackObject]
public class LoRASelectorXL
{
    [Key(0)]
    public Nodetool.Types.Core.HFLoraSDXL lora1 { get; set; } = new Nodetool.Types.Core.HFLoraSDXL();
    [Key(1)]
    public Nodetool.Types.Core.HFLoraSDXL lora2 { get; set; } = new Nodetool.Types.Core.HFLoraSDXL();
    [Key(2)]
    public Nodetool.Types.Core.HFLoraSDXL lora3 { get; set; } = new Nodetool.Types.Core.HFLoraSDXL();
    [Key(3)]
    public Nodetool.Types.Core.HFLoraSDXL lora4 { get; set; } = new Nodetool.Types.Core.HFLoraSDXL();
    [Key(4)]
    public Nodetool.Types.Core.HFLoraSDXL lora5 { get; set; } = new Nodetool.Types.Core.HFLoraSDXL();
    [Key(5)]
    public double strength1 { get; set; } = 1.0;
    [Key(6)]
    public double strength2 { get; set; } = 1.0;
    [Key(7)]
    public double strength3 { get; set; } = 1.0;
    [Key(8)]
    public double strength4 { get; set; } = 1.0;
    [Key(9)]
    public double strength5 { get; set; } = 1.0;

    public object Process()
    {
        return default(object);
    }
}
