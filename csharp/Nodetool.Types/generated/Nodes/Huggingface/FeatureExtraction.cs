using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Huggingface;

[MessagePackObject]
public class FeatureExtraction
{
    [Key(0)]
    public Nodetool.Types.HFFeatureExtraction model { get; set; } = new Nodetool.Types.HFFeatureExtraction();
    [Key(1)]
    public string inputs { get; set; } = "";

    public Nodetool.Types.NPArray Process()
    {
        return default(Nodetool.Types.NPArray);
    }
}
