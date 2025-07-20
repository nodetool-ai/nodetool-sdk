using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types.Core;

[MessagePackObject]
public class ComfyData
{
    [Key(0)]
    public string type { get; set; }
    [Key(1)]
    public object data { get; set; } = null;
}
