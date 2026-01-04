using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class SaveVideoFile
{
    [Key(0)]
    public string filename { get; set; } = @"video.mp4";
    [Key(1)]
    public string folder { get; set; } = @".";
    [Key(2)]
    public bool overwrite { get; set; } = false;
    [Key(3)]
    public Nodetool.Types.Core.VideoRef video { get; set; } = new Nodetool.Types.Core.VideoRef();

    public Nodetool.Types.Core.VideoRef Process()
    {
        return default(Nodetool.Types.Core.VideoRef);
    }
}
