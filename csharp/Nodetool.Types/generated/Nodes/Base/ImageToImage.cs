using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class ImageToImage
{
    [Key(0)]
    public Nodetool.Types.InferenceProviderImageToImageModel model { get; set; } = new Nodetool.Types.InferenceProviderImageToImageModel();
    [Key(1)]
    public Nodetool.Types.ImageRef image { get; set; } = new Nodetool.Types.ImageRef();
    [Key(2)]
    public string prompt { get; set; } = "";
    [Key(3)]
    public double guidance_scale { get; set; } = 7.5;
    [Key(4)]
    public string negative_prompt { get; set; } = "";
    [Key(5)]
    public int num_inference_steps { get; set; } = 30;
    [Key(6)]
    public int target_width { get; set; } = 512;
    [Key(7)]
    public int target_height { get; set; } = 512;

    public Nodetool.Types.ImageRef Process()
    {
        return default(Nodetool.Types.ImageRef);
    }
}
