using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Huggingface;

[MessagePackObject]
public class Wan_T2V
{
    [Key(0)]
    public bool enable_cpu_offload { get; set; } = true;
    [Key(1)]
    public bool enable_vae_slicing { get; set; } = true;
    [Key(2)]
    public bool enable_vae_tiling { get; set; } = false;
    [Key(3)]
    public int fps { get; set; } = 16;
    [Key(4)]
    public double guidance_scale { get; set; } = 5.0;
    [Key(5)]
    public int height { get; set; } = 480;
    [Key(6)]
    public int max_sequence_length { get; set; } = 512;
    [Key(7)]
    public object model_variant { get; set; } = @"Wan-AI/Wan2.2-T2V-A14B-Diffusers";
    [Key(8)]
    public string negative_prompt { get; set; } = @"";
    [Key(9)]
    public int num_frames { get; set; } = 49;
    [Key(10)]
    public int num_inference_steps { get; set; } = 30;
    [Key(11)]
    public string prompt { get; set; } = @"A robot standing on a mountain top at sunset, cinematic lighting, high detail";
    [Key(12)]
    public int seed { get; set; } = -1;
    [Key(13)]
    public int width { get; set; } = 720;
}
