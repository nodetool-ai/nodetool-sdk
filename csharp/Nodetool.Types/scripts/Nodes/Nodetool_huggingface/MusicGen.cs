using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Nodetool_huggingface;

[MessagePackObject]
public class MusicGen
{
    [Key(0)]
    public Nodetool.Types.HFTextToAudio model { get; set; } = new Nodetool.Types.HFTextToAudio();
    [Key(1)]
    public string prompt { get; set; } = "";
    [Key(2)]
    public int max_new_tokens { get; set; } = 1024;

    public Nodetool.Types.AudioRef Process()
    {
        // Implementation would be generated based on node logic
        return default(Nodetool.Types.AudioRef);
    }
}
