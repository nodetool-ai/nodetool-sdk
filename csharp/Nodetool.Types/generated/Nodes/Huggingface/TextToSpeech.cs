using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Huggingface;

[MessagePackObject]
public class TextToSpeech
{
    [Key(0)]
    public Nodetool.Types.Core.HFTextToSpeech model { get; set; } = new Nodetool.Types.Core.HFTextToSpeech();
    [Key(1)]
    public string text { get; set; } = @"Hello, this is a test of the text-to-speech system.";
}
