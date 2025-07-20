using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types.Core;

[MessagePackObject]
public class AssetRef
{
    [Key(0)]
    public string type { get; set; } = "asset";
    [Key(1)]
    public string uri { get; set; } = "";
    [Key(2)]
    public object asset_id { get; set; } = null;
    [Key(3)]
    public object data { get; set; } = null;
}
