using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Core;

[MessagePackObject]
public class DateDifference
{
    [Key(0)]
    public Nodetool.Types.Datetime start_date { get; set; } = new Nodetool.Types.Datetime();
    [Key(1)]
    public Nodetool.Types.Datetime end_date { get; set; } = new Nodetool.Types.Datetime();

    [MessagePackObject]
    public class DateDifferenceOutput
    {
        [Key(0)]
        public int total_seconds { get; set; }
        [Key(1)]
        public int days { get; set; }
        [Key(2)]
        public int hours { get; set; }
        [Key(3)]
        public int minutes { get; set; }
        [Key(4)]
        public int seconds { get; set; }
    }

    public DateDifferenceOutput Process()
    {
        // Implementation would be generated based on node logic
        return new DateDifferenceOutput();
    }
}
