using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class RealtimeAudioInput
{
    [Key(0)]
    public Nodetool.Types.Core.AudioRef audio { get; set; } = new Nodetool.Types.Core.AudioRef();
    [Key(1)]
    public string description { get; set; } = @"";
    [Key(2)]
    public string name { get; set; } = @"";
    [Key(3)]
    public object value { get; set; } = null;

    public Nodetool.Types.Core.Chunk Process()
    {
        return default(Nodetool.Types.Core.Chunk);
    }
}
