using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Huggingface;

[MessagePackObject]
public class ZeroShotAudioClassifier
{
    [Key(0)]
    public Nodetool.Types.Core.AudioRef audio { get; set; } = new Nodetool.Types.Core.AudioRef();
    [Key(1)]
    public string candidate_labels { get; set; } = @"";
    [Key(2)]
    public Nodetool.Types.Core.HFZeroShotAudioClassification model { get; set; } = new Nodetool.Types.Core.HFZeroShotAudioClassification();
}
