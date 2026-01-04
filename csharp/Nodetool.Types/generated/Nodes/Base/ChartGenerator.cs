using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class ChartGenerator
{
    [Key(0)]
    public Nodetool.Types.Core.DataframeRef data { get; set; } = new Nodetool.Types.Core.DataframeRef();
    [Key(1)]
    public int max_tokens { get; set; } = 4096;
    [Key(2)]
    public Nodetool.Types.Core.LanguageModel model { get; set; } = new Nodetool.Types.Core.LanguageModel();
    [Key(3)]
    public string prompt { get; set; } = @"";

    public Nodetool.Types.Core.PlotlyConfig Process()
    {
        return default(Nodetool.Types.Core.PlotlyConfig);
    }
}
