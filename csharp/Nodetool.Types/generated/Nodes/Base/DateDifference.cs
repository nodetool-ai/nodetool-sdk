using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class DateDifference
{
    [Key(0)]
    public Nodetool.Types.Core.Datetime end_date { get; set; } = new Nodetool.Types.Core.Datetime();
    [Key(1)]
    public Nodetool.Types.Core.Datetime start_date { get; set; } = new Nodetool.Types.Core.Datetime();

    [MessagePackObject]
    public class DateDifferenceOutput
    {
        [Key(0)]
        public int days { get; set; }
        [Key(1)]
        public int hours { get; set; }
        [Key(2)]
        public int minutes { get; set; }
        [Key(3)]
        public int seconds { get; set; }
        [Key(4)]
        public int total_seconds { get; set; }
    }

    public DateDifferenceOutput Process()
    {
        return new DateDifferenceOutput();
    }
}
