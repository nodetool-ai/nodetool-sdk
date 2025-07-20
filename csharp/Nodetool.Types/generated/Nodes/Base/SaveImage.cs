using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Image;

[MessagePackObject]
public class SaveImage
{
    [Key(0)]
    public Nodetool.Types.ImageRef image { get; set; } = new Nodetool.Types.ImageRef();
    [Key(1)]
    public Nodetool.Types.FolderRef folder { get; set; } = new Nodetool.Types.FolderRef();
    [Key(2)]
    public string name { get; set; } = "%Y-%m-%d_%H-%M-%S.png";

    public Nodetool.Types.ImageRef Process()
    {
        // Implementation would be generated based on node logic
        return default(Nodetool.Types.ImageRef);
    }
}
