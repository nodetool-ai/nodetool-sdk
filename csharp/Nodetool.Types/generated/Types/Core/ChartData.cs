using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types.Core;

[MessagePackObject]
public class ChartData
{
    [Key(0)]
    public object type { get; set; } = "chart_data";
    [Key(1)]
    public List<Nodetool.Types.Core.DataSeries> series { get; set; } = new();
    [Key(2)]
    public object row { get; set; } = null;
    [Key(3)]
    public object col { get; set; } = null;
    [Key(4)]
    public object col_wrap { get; set; } = null;
}


