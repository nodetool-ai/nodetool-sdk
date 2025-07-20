using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Nodetool_huggingface;

[MessagePackObject]
public class TokenClassification
{
    [Key(0)]
    public Nodetool.Types.HFTokenClassification model { get; set; } = new Nodetool.Types.HFTokenClassification();
    [Key(1)]
    public string inputs { get; set; } = "";
    [Key(2)]
    public object aggregation_strategy { get; set; } = "AggregationStrategy.SIMPLE";

    public Nodetool.Types.DataframeRef Process()
    {
        // Implementation would be generated based on node logic
        return default(Nodetool.Types.DataframeRef);
    }
}
