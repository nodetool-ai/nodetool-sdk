using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Nodetool_base;

[MessagePackObject]
public class AudioOutput
{
    [Key(0)]
    public Nodetool.Types.AudioRef value { get; set; } = new Nodetool.Types.AudioRef();
    [Key(1)]
    public string name { get; set; } = "";
    [Key(2)]
    public string description { get; set; } = "";

    public Nodetool.Types.AudioRef Process()
    {
        // Implementation would be generated based on node logic
        return default(Nodetool.Types.AudioRef);
    }
}
