using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Huggingface;

[MessagePackObject]
public class MusicGen
{
    [Key(0)]
    public int max_new_tokens { get; set; } = 1024;
    [Key(1)]
    public Nodetool.Types.Core.HFTextToAudio model { get; set; } = new Nodetool.Types.Core.HFTextToAudio();
    [Key(2)]
    public string prompt { get; set; } = @"";
}
