using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Huggingface;

[MessagePackObject]
public class KokoroTTS
{
    [Key(0)]
    public object lang_code { get; set; } = @"a";
    [Key(1)]
    public Nodetool.Types.Core.HFTextToSpeech model { get; set; } = new Nodetool.Types.Core.HFTextToSpeech();
    [Key(2)]
    public double speed { get; set; } = 1.0;
    [Key(3)]
    public string text { get; set; } = @"Hello from Kokoro.";
    [Key(4)]
    public object voice { get; set; } = @"af_heart";
}
