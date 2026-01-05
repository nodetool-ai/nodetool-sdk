using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Huggingface;

[MessagePackObject]
public class MusicLDM
{
    [Key(0)]
    public double audio_length_in_s { get; set; } = 5.0;
    [Key(1)]
    public Nodetool.Types.Core.HFTextToAudio model { get; set; } = new Nodetool.Types.Core.HFTextToAudio();
    [Key(2)]
    public int num_inference_steps { get; set; } = 10;
    [Key(3)]
    public string prompt { get; set; } = @"";
}
