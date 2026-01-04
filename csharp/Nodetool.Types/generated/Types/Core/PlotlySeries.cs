using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types.Core;

[MessagePackObject]
public class PlotlySeries
{
    [Key(0)]
    public string chart_type { get; set; }
    [Key(1)]
    public object color { get; set; } = null;
    [Key(2)]
    public object line_dash { get; set; } = null;
    [Key(3)]
    public string name { get; set; }
    [Key(4)]
    public object size { get; set; } = null;
    [Key(5)]
    public object symbol { get; set; } = null;
    [Key(6)]
    public object type { get; set; } = @"plotly_series";
    [Key(7)]
    public string x { get; set; }
    [Key(8)]
    public object y { get; set; } = null;
}
