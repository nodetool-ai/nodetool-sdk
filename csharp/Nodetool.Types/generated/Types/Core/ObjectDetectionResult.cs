using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types.Core;

[MessagePackObject]
public class ObjectDetectionResult
{
    [Key(0)]
    public object type { get; set; } = "object_detection_result";
    [Key(1)]
    public string label { get; set; }
    [Key(2)]
    public double score { get; set; }
    [Key(3)]
    public Nodetool.Types.Core.BoundingBox box { get; set; }
}

