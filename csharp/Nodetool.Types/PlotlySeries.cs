using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types;

[MessagePackObject]
public class PlotlySeries
{
    [Key(0)]
    public object type { get; set; } = "plotly_series";
    [Key(1)]
    public string name { get; set; }
    [Key(2)]
    public string x { get; set; }
    [Key(3)]
    public object y { get; set; } = null;
    [Key(4)]
    public object color { get; set; } = null;
    [Key(5)]
    public object size { get; set; } = null;
    [Key(6)]
    public object symbol { get; set; } = null;
    [Key(7)]
    public object line_dash { get; set; } = null;
    [Key(8)]
    public string chart_type { get; set; }
}
