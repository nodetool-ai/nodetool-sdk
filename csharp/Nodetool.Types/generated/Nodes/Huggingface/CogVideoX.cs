using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Huggingface;

[MessagePackObject]
public class CogVideoX
{
    [Key(0)]
    public bool enable_cpu_offload { get; set; } = true;
    [Key(1)]
    public bool enable_vae_slicing { get; set; } = true;
    [Key(2)]
    public bool enable_vae_tiling { get; set; } = true;
    [Key(3)]
    public int fps { get; set; } = 8;
    [Key(4)]
    public double guidance_scale { get; set; } = 6.0;
    [Key(5)]
    public int height { get; set; } = 480;
    [Key(6)]
    public int max_sequence_length { get; set; } = 226;
    [Key(7)]
    public string negative_prompt { get; set; } = @"";
    [Key(8)]
    public int num_frames { get; set; } = 49;
    [Key(9)]
    public int num_inference_steps { get; set; } = 50;
    [Key(10)]
    public string prompt { get; set; } = @"A detailed wooden toy ship with intricately carved masts and sails is seen gliding smoothly over a plush, blue carpet that mimics the waves of the sea. The ship's hull is painted a rich brown, with tiny windows. The carpet, soft and textured, provides a perfect backdrop, resembling an oceanic expanse. Surrounding the ship are various other toys and children's items, hinting at a playful environment. The scene captures the innocence and imagination of childhood, with the toy ship's journey symbolizing endless adventures in a whimsical, indoor setting.";
    [Key(11)]
    public int seed { get; set; } = -1;
    [Key(12)]
    public int width { get; set; } = 720;
}
