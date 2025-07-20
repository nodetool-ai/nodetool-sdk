using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Core;

[MessagePackObject]
public class ImageOutput
{
    [Key(0)]
    public Nodetool.Types.ImageRef value { get; set; } = new Nodetool.Types.ImageRef();
    [Key(1)]
    public string name { get; set; } = "";
    [Key(2)]
    public string description { get; set; } = "";

    public Nodetool.Types.ImageRef Process()
    {
        // Implementation would be generated based on node logic
        return default(Nodetool.Types.ImageRef);
    }
}
