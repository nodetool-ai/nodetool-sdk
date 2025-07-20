using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Video;

[MessagePackObject]
public class SaveVideo
{
    [Key(0)]
    public Nodetool.Types.VideoRef video { get; set; } = new Nodetool.Types.VideoRef();
    [Key(1)]
    public Nodetool.Types.FolderRef folder { get; set; } = new Nodetool.Types.FolderRef();
    [Key(2)]
    public string name { get; set; } = "%Y-%m-%d-%H-%M-%S.mp4";

    public Nodetool.Types.VideoRef Process()
    {
        // Implementation would be generated based on node logic
        return default(Nodetool.Types.VideoRef);
    }
}
