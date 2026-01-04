using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class ImageToImage
{
    [Key(0)]
    public double guidance_scale { get; set; } = 7.5;
    [Key(1)]
    public Nodetool.Types.Core.ImageRef image { get; set; } = new Nodetool.Types.Core.ImageRef();
    [Key(2)]
    public Nodetool.Types.Core.ImageModel model { get; set; } = new Nodetool.Types.Core.ImageModel();
    [Key(3)]
    public string negative_prompt { get; set; } = @"";
    [Key(4)]
    public int num_inference_steps { get; set; } = 30;
    [Key(5)]
    public string prompt { get; set; } = @"A photorealistic version of the input image";
    [Key(6)]
    public bool safety_check { get; set; } = true;
    [Key(7)]
    public string scheduler { get; set; } = @"";
    [Key(8)]
    public int seed { get; set; } = -1;
    [Key(9)]
    public double strength { get; set; } = 0.8;
    [Key(10)]
    public int target_height { get; set; } = 512;
    [Key(11)]
    public int target_width { get; set; } = 512;

    public Nodetool.Types.Core.ImageRef Process()
    {
        return default(Nodetool.Types.Core.ImageRef);
    }
}
