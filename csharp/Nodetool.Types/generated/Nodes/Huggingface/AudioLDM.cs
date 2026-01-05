using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Huggingface;

[MessagePackObject]
public class AudioLDM
{
    [Key(0)]
    public double audio_length_in_s { get; set; } = 5.0;
    [Key(1)]
    public int num_inference_steps { get; set; } = 10;
    [Key(2)]
    public string prompt { get; set; } = @"Techno music with a strong, upbeat tempo and high melodic riffs";
    [Key(3)]
    public int seed { get; set; } = 0;
}
