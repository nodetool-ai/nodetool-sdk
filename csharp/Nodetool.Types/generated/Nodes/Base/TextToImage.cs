using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class TextToImage
{
    [Key(0)]
    public double guidance_scale { get; set; } = 7.5;
    [Key(1)]
    public int height { get; set; } = 512;
    [Key(2)]
    public Nodetool.Types.Core.ImageModel model { get; set; } = new Nodetool.Types.Core.ImageModel();
    [Key(3)]
    public string negative_prompt { get; set; } = @"";
    [Key(4)]
    public int num_inference_steps { get; set; } = 30;
    [Key(5)]
    public string prompt { get; set; } = @"A cat holding a sign that says hello world";
    [Key(6)]
    public bool safety_check { get; set; } = true;
    [Key(7)]
    public int seed { get; set; } = -1;
    [Key(8)]
    public int width { get; set; } = 512;

    public Nodetool.Types.Core.ImageRef Process()
    {
        return default(Nodetool.Types.Core.ImageRef);
    }
}
