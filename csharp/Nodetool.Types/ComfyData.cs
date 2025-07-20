using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types;

[MessagePackObject]
public class ComfyData
{
    [Key(0)]
    public string type { get; set; }
    [Key(1)]
    public object data { get; set; } = null;
}
