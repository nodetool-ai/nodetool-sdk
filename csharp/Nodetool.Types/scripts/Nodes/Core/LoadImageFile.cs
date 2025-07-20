using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Core;

[MessagePackObject]
public class LoadImageFile
{
    [Key(0)]
    public Nodetool.Types.FilePath path { get; set; } = new Nodetool.Types.FilePath();

    public Nodetool.Types.ImageRef Process()
    {
        // Implementation would be generated based on node logic
        return default(Nodetool.Types.ImageRef);
    }
}
