using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types.Core;

[MessagePackObject]
public class SVGRef
{
    [Key(0)]
    public object asset_id { get; set; } = null;
    [Key(1)]
    public object data { get; set; } = null;
    [Key(2)]
    public object type { get; set; } = @"svg";
    [Key(3)]
    public string uri { get; set; } = @"";
}
