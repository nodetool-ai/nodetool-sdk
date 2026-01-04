using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Huggingface;

[MessagePackObject]
public class AudioClassifier
{
    [Key(0)]
    public Nodetool.Types.Core.AudioRef audio { get; set; } = new Nodetool.Types.Core.AudioRef();
    [Key(1)]
    public Nodetool.Types.Core.HFAudioClassification model { get; set; } = new Nodetool.Types.Core.HFAudioClassification();
    [Key(2)]
    public int top_k { get; set; } = 10;
}
