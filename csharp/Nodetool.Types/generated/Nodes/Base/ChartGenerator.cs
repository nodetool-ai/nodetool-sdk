using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Generators;

[MessagePackObject]
public class ChartGenerator
{
    [Key(0)]
    public Nodetool.Types.LanguageModel model { get; set; } = new Nodetool.Types.LanguageModel();
    [Key(1)]
    public string prompt { get; set; } = "";
    [Key(2)]
    public Nodetool.Types.DataframeRef data { get; set; } = new Nodetool.Types.DataframeRef();
    [Key(3)]
    public int max_tokens { get; set; } = 4096;

    public Nodetool.Types.PlotlyConfig Process()
    {
        // Implementation would be generated based on node logic
        return default(Nodetool.Types.PlotlyConfig);
    }
}
