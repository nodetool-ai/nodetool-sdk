using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Nodetool_base;

[MessagePackObject]
public class SaveVideoFile
{
    [Key(0)]
    public Nodetool.Types.VideoRef video { get; set; } = new Nodetool.Types.VideoRef();
    [Key(1)]
    public Nodetool.Types.FolderPath folder { get; set; } = new Nodetool.Types.FolderPath();
    [Key(2)]
    public string filename { get; set; } = "";

    public Nodetool.Types.VideoRef Process()
    {
        // Implementation would be generated based on node logic
        return default(Nodetool.Types.VideoRef);
    }
}
