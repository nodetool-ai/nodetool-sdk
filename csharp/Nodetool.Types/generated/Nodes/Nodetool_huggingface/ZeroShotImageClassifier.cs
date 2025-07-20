using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Nodetool_huggingface;

[MessagePackObject]
public class ZeroShotImageClassifier
{
    [Key(0)]
    public Nodetool.Types.HFZeroShotImageClassification model { get; set; } = new Nodetool.Types.HFZeroShotImageClassification();
    [Key(1)]
    public Nodetool.Types.ImageRef image { get; set; } = new Nodetool.Types.ImageRef();
    [Key(2)]
    public string candidate_labels { get; set; } = "";

    public object Process()
    {
        // Implementation would be generated based on node logic
        return default(object);
    }
}
