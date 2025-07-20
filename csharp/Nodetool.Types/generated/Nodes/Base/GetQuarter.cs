using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class GetQuarter
{
    [Key(0)]
    public Nodetool.Types.Datetime input_datetime { get; set; } = new Nodetool.Types.Datetime();

    [MessagePackObject]
    public class GetQuarterOutput
    {
        [Key(0)]
        public int quarter { get; set; }
        [Key(1)]
        public Nodetool.Types.Datetime quarter_start { get; set; }
        [Key(2)]
        public Nodetool.Types.Datetime quarter_end { get; set; }
    }

    public GetQuarterOutput Process()
    {
        return new GetQuarterOutput();
    }
}
