using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Huggingface;

[MessagePackObject]
public class AudioLDM2
{
    [Key(0)]
    public double audio_length_in_s { get; set; } = 10.0;
    [Key(1)]
    public string negative_prompt { get; set; } = @"Low quality.";
    [Key(2)]
    public int num_inference_steps { get; set; } = 200;
    [Key(3)]
    public int num_waveforms_per_prompt { get; set; } = 3;
    [Key(4)]
    public string prompt { get; set; } = @"The sound of a hammer hitting a wooden surface.";
    [Key(5)]
    public int seed { get; set; } = 0;
}
