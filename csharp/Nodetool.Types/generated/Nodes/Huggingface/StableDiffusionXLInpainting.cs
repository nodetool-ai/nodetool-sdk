using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Huggingface;

[MessagePackObject]
public class StableDiffusionXLInpainting
{
    [Key(0)]
    public Nodetool.Types.HFStableDiffusionXL model { get; set; } = new Nodetool.Types.HFStableDiffusionXL();
    [Key(1)]
    public string prompt { get; set; } = "";
    [Key(2)]
    public string negative_prompt { get; set; } = "";
    [Key(3)]
    public int seed { get; set; } = -1;
    [Key(4)]
    public int num_inference_steps { get; set; } = 25;
    [Key(5)]
    public double guidance_scale { get; set; } = 7.0;
    [Key(6)]
    public int width { get; set; } = 1024;
    [Key(7)]
    public int height { get; set; } = 1024;
    [Key(8)]
    public object scheduler { get; set; } = "StableDiffusionScheduler.EulerDiscreteScheduler";
    [Key(9)]
    public object loras { get; set; } = new List<object>();
    [Key(10)]
    public double lora_scale { get; set; } = 0.5;
    [Key(11)]
    public Nodetool.Types.HFIPAdapter ip_adapter_model { get; set; } = new Nodetool.Types.HFIPAdapter();
    [Key(12)]
    public Nodetool.Types.ImageRef ip_adapter_image { get; set; } = new Nodetool.Types.ImageRef();
    [Key(13)]
    public double ip_adapter_scale { get; set; } = 0.5;
    [Key(14)]
    public bool enable_tiling { get; set; } = false;
    [Key(15)]
    public bool enable_cpu_offload { get; set; } = false;
    [Key(16)]
    public Nodetool.Types.ImageRef image { get; set; } = new Nodetool.Types.ImageRef();
    [Key(17)]
    public Nodetool.Types.ImageRef mask_image { get; set; } = new Nodetool.Types.ImageRef();
    [Key(18)]
    public double strength { get; set; } = 0.8;

    public Nodetool.Types.ImageRef Process()
    {
        return default(Nodetool.Types.ImageRef);
    }
}
