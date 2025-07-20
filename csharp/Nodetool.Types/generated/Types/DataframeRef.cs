using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types;

[MessagePackObject]
public class DataframeRef
{
    [Key(0)]
    public object type { get; set; } = "dataframe";
    [Key(1)]
    public string uri { get; set; } = "";
    [Key(2)]
    public object asset_id { get; set; } = null;
    [Key(3)]
    public object data { get; set; } = null;
    [Key(4)]
    public object columns { get; set; } = null;
}
