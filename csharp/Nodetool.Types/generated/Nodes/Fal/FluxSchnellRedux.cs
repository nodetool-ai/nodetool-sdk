using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Fal;

[MessagePackObject]
public class FluxSchnellRedux
{
    [Key(0)]
    public Nodetool.Types.ImageRef image { get; set; } = new Nodetool.Types.ImageRef();
    [Key(1)]
    public object image_size { get; set; } = "ImageSizePreset.LANDSCAPE_4_3";
    [Key(2)]
    public int num_inference_steps { get; set; } = 4;
    [Key(3)]
    public int seed { get; set; } = -1;
    [Key(4)]
    public bool enable_safety_checker { get; set; } = true;

    public Nodetool.Types.ImageRef Process()
    {
        return default(Nodetool.Types.ImageRef);
    }
}
