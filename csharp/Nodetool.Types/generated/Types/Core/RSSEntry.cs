using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types.Core;

[MessagePackObject]
public class RSSEntry
{
    [Key(0)]
    public string author { get; set; } = @"";
    [Key(1)]
    public string link { get; set; } = @"";
    [Key(2)]
    public Nodetool.Types.Core.Datetime published { get; set; } = new Nodetool.Types.Core.Datetime();
    [Key(3)]
    public string summary { get; set; } = @"";
    [Key(4)]
    public string title { get; set; } = @"";
    [Key(5)]
    public object type { get; set; } = @"rss_entry";
}
