using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Nodetool_base;

[MessagePackObject]
public class AudioClassification
{
    [Key(0)]
    public Nodetool.Types.InferenceProviderAudioClassificationModel model { get; set; } = new Nodetool.Types.InferenceProviderAudioClassificationModel();
    [Key(1)]
    public Nodetool.Types.AudioRef audio { get; set; } = new Nodetool.Types.AudioRef();
    [Key(2)]
    public object function_to_apply { get; set; }
    [Key(3)]
    public int top_k { get; set; } = 1;

    public object Process()
    {
        // Implementation would be generated based on node logic
        return default(object);
    }
}
