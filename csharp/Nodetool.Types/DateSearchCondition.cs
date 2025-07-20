using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types;

[MessagePackObject]
public class DateSearchCondition
{
    [Key(0)]
    public object type { get; set; } = "date_search_condition";
    [Key(1)]
    public object criteria { get; set; }
    [Key(2)]
    public Nodetool.Types.Datetime date { get; set; }
}
