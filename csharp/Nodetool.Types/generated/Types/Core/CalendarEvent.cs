using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types.Core;

[MessagePackObject]
public class CalendarEvent
{
    [Key(0)]
    public string calendar { get; set; } = @"";
    [Key(1)]
    public Nodetool.Types.Core.Datetime end_date { get; set; } = new Nodetool.Types.Core.Datetime();
    [Key(2)]
    public string location { get; set; } = @"";
    [Key(3)]
    public string notes { get; set; } = @"";
    [Key(4)]
    public Nodetool.Types.Core.Datetime start_date { get; set; } = new Nodetool.Types.Core.Datetime();
    [Key(5)]
    public string title { get; set; } = @"";
    [Key(6)]
    public object type { get; set; } = @"calendar_event";
}
