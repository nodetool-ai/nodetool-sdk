using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Huggingface;

[MessagePackObject]
public class Whisper
{
    [Key(0)]
    public Nodetool.Types.Core.AudioRef audio { get; set; } = new Nodetool.Types.Core.AudioRef();
    [Key(1)]
    public object language { get; set; } = @"auto_detect";
    [Key(2)]
    public Nodetool.Types.Core.HFAutomaticSpeechRecognition model { get; set; } = new Nodetool.Types.Core.HFAutomaticSpeechRecognition();
    [Key(3)]
    public object task { get; set; } = @"transcribe";
    [Key(4)]
    public object timestamps { get; set; } = @"none";
}
