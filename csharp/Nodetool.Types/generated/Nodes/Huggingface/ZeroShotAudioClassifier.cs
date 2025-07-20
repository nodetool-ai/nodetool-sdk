using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Huggingface;

[MessagePackObject]
public class ZeroShotAudioClassifier
{
    [Key(0)]
    public Nodetool.Types.HFZeroShotAudioClassification model { get; set; } = new Nodetool.Types.HFZeroShotAudioClassification();
    [Key(1)]
    public Nodetool.Types.AudioRef audio { get; set; } = new Nodetool.Types.AudioRef();
    [Key(2)]
    public string candidate_labels { get; set; } = "";

    public object Process()
    {
        return default(object);
    }
}
