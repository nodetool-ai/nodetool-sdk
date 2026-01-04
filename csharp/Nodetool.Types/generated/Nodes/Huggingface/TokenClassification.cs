using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Huggingface;

[MessagePackObject]
public class TokenClassification
{
    [Key(0)]
    public object aggregation_strategy { get; set; } = @"simple";
    [Key(1)]
    public string inputs { get; set; } = @"";
    [Key(2)]
    public Nodetool.Types.Core.HFTokenClassification model { get; set; } = new Nodetool.Types.Core.HFTokenClassification();

    public Nodetool.Types.Core.DataframeRef Process()
    {
        return default(Nodetool.Types.Core.DataframeRef);
    }
}
