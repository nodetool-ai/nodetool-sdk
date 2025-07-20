using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Image;

[MessagePackObject]
public class Crop
{
    [Key(0)]
    public Nodetool.Types.ImageRef image { get; set; } = new Nodetool.Types.ImageRef();
    [Key(1)]
    public int left { get; set; } = 0;
    [Key(2)]
    public int top { get; set; } = 0;
    [Key(3)]
    public int right { get; set; } = 512;
    [Key(4)]
    public int bottom { get; set; } = 512;

    public Nodetool.Types.ImageRef Process()
    {
        // Implementation would be generated based on node logic
        return default(Nodetool.Types.ImageRef);
    }
}
