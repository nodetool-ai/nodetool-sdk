using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class IsDateInRange
{
    [Key(0)]
    public Nodetool.Types.Datetime check_date { get; set; } = new Nodetool.Types.Datetime();
    [Key(1)]
    public Nodetool.Types.Datetime start_date { get; set; } = new Nodetool.Types.Datetime();
    [Key(2)]
    public Nodetool.Types.Datetime end_date { get; set; } = new Nodetool.Types.Datetime();
    [Key(3)]
    public bool inclusive { get; set; } = true;

    public bool Process()
    {
        return default(bool);
    }
}
