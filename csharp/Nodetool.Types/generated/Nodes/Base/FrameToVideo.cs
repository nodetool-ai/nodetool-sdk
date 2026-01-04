using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class FrameToVideo
{
    [Key(0)]
    public double fps { get; set; } = 30;
    [Key(1)]
    public Nodetool.Types.Core.ImageRef frame { get; set; } = new Nodetool.Types.Core.ImageRef();

    public void Process()
    {
    }
}
