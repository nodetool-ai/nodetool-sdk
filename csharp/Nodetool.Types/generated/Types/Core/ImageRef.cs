using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types.Core;

[MessagePackObject]
public class ImageRef
{
    [Key(0)]
    public object type { get; set; } = "image";
    [Key(1)]
    public string uri { get; set; } = "";
    [Key(2)]
    public object asset_id { get; set; } = null;
    [Key(3)]
    public object data { get; set; } = null;
}
