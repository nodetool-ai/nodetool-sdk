using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Huggingface;

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
        return default(Nodetool.Types.DataframeRef);
    }
}
