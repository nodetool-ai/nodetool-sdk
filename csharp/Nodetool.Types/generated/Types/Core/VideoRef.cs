using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types.Core;

[MessagePackObject]
public class VideoRef
{
    [Key(0)]
    public object asset_id { get; set; } = null;
    [Key(1)]
    public object data { get; set; } = null;
    [Key(2)]
    public double? duration { get; set; } = null;
    [Key(3)]
    public string? format { get; set; } = null;
    [Key(4)]
    public object type { get; set; } = @"video";
    [Key(5)]
    public string uri { get; set; } = @"";
}
