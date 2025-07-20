using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types.Core;

[MessagePackObject]
public class LogEntry
{
    [Key(0)]
    public object type { get; set; } = "log_entry";
    [Key(1)]
    public string message { get; set; } = "";
    [Key(2)]
    public object level { get; set; } = "info";
    [Key(3)]
    public int timestamp { get; set; } = 0;
}
