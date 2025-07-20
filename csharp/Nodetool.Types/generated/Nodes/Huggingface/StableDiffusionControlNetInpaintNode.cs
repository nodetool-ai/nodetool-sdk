using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Huggingface;

[MessagePackObject]
public class StableDiffusionControlNetInpaintNode
{
    [Key(0)]
    public Nodetool.Types.HFStableDiffusion model { get; set; } = new Nodetool.Types.HFStableDiffusion();
    [Key(1)]
    public string prompt { get; set; } = "";
    [Key(2)]
    public string negative_prompt { get; set; } = "(blurry, low quality, deformed, mutated, bad anatomy, extra limbs, bad proportions, text, watermark, grainy, pixelated, disfigured face, missing fingers, cropped image, bad lighting";
    [Key(3)]
    public int seed { get; set; } = -1;
    [Key(4)]
    public int num_inference_steps { get; set; } = 25;
    [Key(5)]
    public double guidance_scale { get; set; } = 7.5;
    [Key(6)]
    public object scheduler { get; set; } = "StableDiffusionScheduler.EulerDiscreteScheduler";
    [Key(7)]
    public object loras { get; set; } = new List<object>();
    [Key(8)]
    public Nodetool.Types.HFIPAdapter ip_adapter_model { get; set; } = new Nodetool.Types.HFIPAdapter();
    [Key(9)]
    public Nodetool.Types.ImageRef ip_adapter_image { get; set; } = new Nodetool.Types.ImageRef();
    [Key(10)]
    public double ip_adapter_scale { get; set; } = 0.5;
    [Key(11)]
    public double detail_level { get; set; } = 0.5;
    [Key(12)]
    public bool enable_tiling { get; set; } = false;
    [Key(13)]
    public bool enable_cpu_offload { get; set; } = false;
    [Key(14)]
    public object upscaler { get; set; } = "StableDiffusionUpscaler.NONE";
    [Key(15)]
    public object controlnet { get; set; } = "StableDiffusionControlNetModel.INPAINT";
    [Key(16)]
    public Nodetool.Types.ImageRef init_image { get; set; } = new Nodetool.Types.ImageRef();
    [Key(17)]
    public Nodetool.Types.ImageRef mask_image { get; set; } = new Nodetool.Types.ImageRef();
    [Key(18)]
    public Nodetool.Types.ImageRef control_image { get; set; } = new Nodetool.Types.ImageRef();
    [Key(19)]
    public double controlnet_conditioning_scale { get; set; } = 0.5;

    public Nodetool.Types.ImageRef Process()
    {
        return default(Nodetool.Types.ImageRef);
    }
}
