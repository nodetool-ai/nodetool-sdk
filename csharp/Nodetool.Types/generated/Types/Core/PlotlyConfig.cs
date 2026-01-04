using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types.Core;

[MessagePackObject]
public class PlotlyConfig
{
    [Key(0)]
    public object type { get; set; } = "plotly_config";
    [Key(1)]
    public Dictionary<string, object> config { get; set; } = new();
}

