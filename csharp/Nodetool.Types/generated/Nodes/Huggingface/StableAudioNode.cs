using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Huggingface;

[MessagePackObject]
public class StableAudioNode
{
    [Key(0)]
    public string prompt { get; set; } = "A peaceful piano melody.";
    [Key(1)]
    public string negative_prompt { get; set; } = "Low quality.";
    [Key(2)]
    public double duration { get; set; } = 10.0;
    [Key(3)]
    public int num_inference_steps { get; set; } = 200;
    [Key(4)]
    public int seed { get; set; } = 0;

    public Nodetool.Types.AudioRef Process()
    {
        return default(Nodetool.Types.AudioRef);
    }
}
