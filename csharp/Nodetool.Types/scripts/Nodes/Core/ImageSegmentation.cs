using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Core;

[MessagePackObject]
public class ImageSegmentation
{
    [Key(0)]
    public Nodetool.Types.InferenceProviderImageSegmentationModel model { get; set; } = new Nodetool.Types.InferenceProviderImageSegmentationModel();
    [Key(1)]
    public Nodetool.Types.ImageRef image { get; set; } = new Nodetool.Types.ImageRef();
    [Key(2)]
    public double mask_threshold { get; set; } = 0.5;
    [Key(3)]
    public double overlap_mask_area_threshold { get; set; } = 0.5;
    [Key(4)]
    public object subtask { get; set; } = "Subtask.semantic";
    [Key(5)]
    public double threshold { get; set; } = 0.5;

    public object Process()
    {
        // Implementation would be generated based on node logic
        return default(object);
    }
}
