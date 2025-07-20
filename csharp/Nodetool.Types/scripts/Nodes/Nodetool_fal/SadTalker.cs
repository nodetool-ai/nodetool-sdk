using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Nodetool_fal;

[MessagePackObject]
public class SadTalker
{
    [Key(0)]
    public Nodetool.Types.ImageRef image { get; set; } = new Nodetool.Types.ImageRef();
    [Key(1)]
    public string audio { get; set; } = "";
    [Key(2)]
    public object face_model_resolution { get; set; }
    [Key(3)]
    public double expression_scale { get; set; } = 1.0;
    [Key(4)]
    public bool still_mode { get; set; } = false;
    [Key(5)]
    public object preprocess { get; set; }

    public Nodetool.Types.VideoRef Process()
    {
        // Implementation would be generated based on node logic
        return default(Nodetool.Types.VideoRef);
    }
}
