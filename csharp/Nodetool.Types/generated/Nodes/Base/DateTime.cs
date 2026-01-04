using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class DateTime
{
    [Key(0)]
    public int day { get; set; } = 1;
    [Key(1)]
    public int hour { get; set; } = 0;
    [Key(2)]
    public int microsecond { get; set; } = 0;
    [Key(3)]
    public int minute { get; set; } = 0;
    [Key(4)]
    public int month { get; set; } = 1;
    [Key(5)]
    public int second { get; set; } = 0;
    [Key(6)]
    public string tzinfo { get; set; } = @"UTC";
    [Key(7)]
    public int utc_offset { get; set; } = 0;
    [Key(8)]
    public int year { get; set; } = 1900;

    public Nodetool.Types.Core.Datetime Process()
    {
        return default(Nodetool.Types.Core.Datetime);
    }
}
