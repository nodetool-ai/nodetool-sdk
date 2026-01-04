using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Fal;

[MessagePackObject]
public class SadTalker
{
    [Key(0)]
    public string audio { get; set; } = @"";
    [Key(1)]
    public double expression_scale { get; set; } = 1.0;
    [Key(2)]
    public object face_model_resolution { get; set; }
    [Key(3)]
    public Nodetool.Types.Core.ImageRef image { get; set; } = new Nodetool.Types.Core.ImageRef();
    [Key(4)]
    public object preprocess { get; set; }
    [Key(5)]
    public bool still_mode { get; set; } = false;

    public Nodetool.Types.Core.VideoRef Process()
    {
        return default(Nodetool.Types.Core.VideoRef);
    }
}
