using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types;

[MessagePackObject]
public class Event
{
    [Key(0)]
    public object type { get; set; } = "event";
    [Key(1)]
    public string name { get; set; } = "";
    [Key(2)]
    public Dictionary<string, object> payload { get; set; } = new Dictionary<string, object>();
}
