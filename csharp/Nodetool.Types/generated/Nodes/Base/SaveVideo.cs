using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class SaveVideo
{
    [Key(0)]
    public Nodetool.Types.Core.FolderRef folder { get; set; } = new Nodetool.Types.Core.FolderRef();
    [Key(1)]
    public string name { get; set; } = @"%Y-%m-%d-%H-%M-%S.mp4";
    [Key(2)]
    public Nodetool.Types.Core.VideoRef video { get; set; } = new Nodetool.Types.Core.VideoRef();

    public Nodetool.Types.Core.VideoRef Process()
    {
        return default(Nodetool.Types.Core.VideoRef);
    }
}
