using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Nodetool_base;

[MessagePackObject]
public class Summarization
{
    [Key(0)]
    public Nodetool.Types.InferenceProviderSummarizationModel model { get; set; } = new Nodetool.Types.InferenceProviderSummarizationModel();
    [Key(1)]
    public string text { get; set; } = "";
    [Key(2)]
    public bool clean_up_tokenization_spaces { get; set; } = true;
    [Key(3)]
    public object truncation { get; set; } = "Truncation.do_not_truncate";

    public string Process()
    {
        // Implementation would be generated based on node logic
        return default(string);
    }
}
