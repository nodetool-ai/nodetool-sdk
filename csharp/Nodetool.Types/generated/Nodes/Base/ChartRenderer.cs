using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class ChartRenderer
{
    [Key(0)]
    public Nodetool.Types.Core.ChartConfig chart_config { get; set; } = new Nodetool.Types.Core.ChartConfig();
    [Key(1)]
    public object data { get; set; } = null;
    [Key(2)]
    public bool despine { get; set; } = true;
    [Key(3)]
    public int height { get; set; } = 480;
    [Key(4)]
    public bool trim_margins { get; set; } = true;
    [Key(5)]
    public int width { get; set; } = 640;

    public Nodetool.Types.Core.ImageRef Process()
    {
        return default(Nodetool.Types.Core.ImageRef);
    }
}
