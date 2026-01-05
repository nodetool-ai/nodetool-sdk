using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types.Core;

[MessagePackObject]
public class DateSearchCondition
{
    [Key(0)]
    public object criteria { get; set; }
    [Key(1)]
    public Nodetool.Types.Core.Datetime date { get; set; }
    [Key(2)]
    public object type { get; set; } = @"date_search_condition";
}
