using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types.Core;

[MessagePackObject]
public class ChartData
{
    [Key(0)]
    public object col { get; set; } = null;
    [Key(1)]
    public object col_wrap { get; set; } = null;
    [Key(2)]
    public object row { get; set; } = null;
    [Key(3)]
    public List<Nodetool.Types.Core.DataSeries> series { get; set; } = new();
    [Key(4)]
    public object type { get; set; } = @"chart_data";
}
