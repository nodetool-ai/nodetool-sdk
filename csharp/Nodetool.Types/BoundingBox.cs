using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types;

[MessagePackObject]
public class BoundingBox
{
    [Key(0)]
    public object type { get; set; } = "bounding_box";
    [Key(1)]
    public double xmin { get; set; }
    [Key(2)]
    public double ymin { get; set; }
    [Key(3)]
    public double xmax { get; set; }
    [Key(4)]
    public double ymax { get; set; }
}
