using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Core;

[MessagePackObject]
public class AudioClassifier
{
    [Key(0)]
    public Nodetool.Types.HFAudioClassification model { get; set; } = new Nodetool.Types.HFAudioClassification();
    [Key(1)]
    public Nodetool.Types.AudioRef audio { get; set; } = new Nodetool.Types.AudioRef();
    [Key(2)]
    public int top_k { get; set; } = 10;

    public object Process()
    {
        // Implementation would be generated based on node logic
        return default(object);
    }
}
