using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types.Core;

[MessagePackObject]
public class Date
{
    [Key(0)]
    public int day { get; set; } = 0;
    [Key(1)]
    public int month { get; set; } = 0;
    [Key(2)]
    public object type { get; set; } = @"date";
    [Key(3)]
    public int year { get; set; } = 0;
}
