using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types;

[MessagePackObject]
public class ImageSegmentationResult
{
    [Key(0)]
    public object type { get; set; } = "image_segmentation_result";
    [Key(1)]
    public string label { get; set; }
    [Key(2)]
    public Nodetool.Types.ImageRef mask { get; set; }
}
