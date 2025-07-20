using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types.Core;

[MessagePackObject]
public class Datetime
{
    [Key(0)]
    public object type { get; set; } = "datetime";
    [Key(1)]
    public int year { get; set; } = 0;
    [Key(2)]
    public int month { get; set; } = 0;
    [Key(3)]
    public int day { get; set; } = 0;
    [Key(4)]
    public int hour { get; set; } = 0;
    [Key(5)]
    public int minute { get; set; } = 0;
    [Key(6)]
    public int second { get; set; } = 0;
    [Key(7)]
    public int microsecond { get; set; } = 0;
    [Key(8)]
    public string tzinfo { get; set; } = "UTC";
    [Key(9)]
    public double utc_offset { get; set; } = 0;
}
