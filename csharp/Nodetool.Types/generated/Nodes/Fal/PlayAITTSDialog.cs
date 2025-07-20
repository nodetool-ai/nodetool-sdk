using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Fal;

[MessagePackObject]
public class PlayAITTSDialog
{
    [Key(0)]
    public string text { get; set; } = "";
    [Key(1)]
    public string voice { get; set; } = "nova";
    [Key(2)]
    public double speed { get; set; } = 1.0;

    public Nodetool.Types.AudioRef Process()
    {
        return default(Nodetool.Types.AudioRef);
    }
}
