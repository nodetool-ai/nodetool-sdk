using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types.Core;

[MessagePackObject]
public class ComfyModel
{
    [Key(0)]
    public string type { get; set; }
    [Key(1)]
    public string name { get; set; } = "";
    [Key(2)]
    public object model { get; set; } = null;
}
