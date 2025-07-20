using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types;

[MessagePackObject]
public class RSSEntry
{
    [Key(0)]
    public object type { get; set; } = "rss_entry";
    [Key(1)]
    public string title { get; set; } = "";
    [Key(2)]
    public string link { get; set; } = "";
    [Key(3)]
    public Nodetool.Types.Datetime published { get; set; } = new Nodetool.Types.Datetime();
    [Key(4)]
    public string summary { get; set; } = "";
    [Key(5)]
    public string author { get; set; } = "";
}
