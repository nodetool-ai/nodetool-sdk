using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Nodetool_huggingface;

[MessagePackObject]
public class Bark
{
    [Key(0)]
    public Nodetool.Types.HFTextToSpeech model { get; set; } = new Nodetool.Types.HFTextToSpeech();
    [Key(1)]
    public string prompt { get; set; } = "";

    public Nodetool.Types.AudioRef Process()
    {
        // Implementation would be generated based on node logic
        return default(Nodetool.Types.AudioRef);
    }
}
