using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class TextToImage
{
    [Key(0)]
    public Nodetool.Types.InferenceProviderTextToImageModel model { get; set; } = new Nodetool.Types.InferenceProviderTextToImageModel();
    [Key(1)]
    public string prompt { get; set; } = "";
    [Key(2)]
    public double guidance_scale { get; set; } = 7.5;
    [Key(3)]
    public string negative_prompt { get; set; } = "";
    [Key(4)]
    public int num_inference_steps { get; set; } = 30;
    [Key(5)]
    public int width { get; set; } = 512;
    [Key(6)]
    public int height { get; set; } = 512;
    [Key(7)]
    public int seed { get; set; } = -1;

    public Nodetool.Types.ImageRef Process()
    {
        return default(Nodetool.Types.ImageRef);
    }
}
