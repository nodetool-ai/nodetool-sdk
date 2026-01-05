using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class IsDateInRange
{
    [Key(0)]
    public Nodetool.Types.Core.Datetime check_date { get; set; } = new Nodetool.Types.Core.Datetime();
    [Key(1)]
    public Nodetool.Types.Core.Datetime end_date { get; set; } = new Nodetool.Types.Core.Datetime();
    [Key(2)]
    public bool inclusive { get; set; } = true;
    [Key(3)]
    public Nodetool.Types.Core.Datetime start_date { get; set; } = new Nodetool.Types.Core.Datetime();

    public bool Process()
    {
        return default(bool);
    }
}
