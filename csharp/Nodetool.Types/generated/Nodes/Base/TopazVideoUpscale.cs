using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class TopazVideoUpscale
{
    [Key(0)]
    public bool denoise { get; set; } = true;
    [Key(1)]
    public object resolution { get; set; } = @"1080p";
    [Key(2)]
    public Nodetool.Types.Core.VideoRef video { get; set; } = new Nodetool.Types.Core.VideoRef();

    public Nodetool.Types.Core.VideoRef Process()
    {
        return default(Nodetool.Types.Core.VideoRef);
    }
}
