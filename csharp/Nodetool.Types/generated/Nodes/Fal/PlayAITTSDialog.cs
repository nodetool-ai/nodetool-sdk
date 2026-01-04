using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Fal;

[MessagePackObject]
public class PlayAITTSDialog
{
    [Key(0)]
    public double speed { get; set; } = 1.0;
    [Key(1)]
    public string text { get; set; } = @"";
    [Key(2)]
    public string voice { get; set; } = @"nova";

    public Nodetool.Types.Core.AudioRef Process()
    {
        return default(Nodetool.Types.Core.AudioRef);
    }
}
