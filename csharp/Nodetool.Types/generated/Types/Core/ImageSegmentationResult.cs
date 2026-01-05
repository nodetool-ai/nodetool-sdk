using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types.Core;

[MessagePackObject]
public class ImageSegmentationResult
{
    [Key(0)]
    public string label { get; set; }
    [Key(1)]
    public Nodetool.Types.Core.ImageRef mask { get; set; }
    [Key(2)]
    public object type { get; set; } = @"image_segmentation_result";
}
