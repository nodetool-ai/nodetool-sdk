using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types.Core;

[MessagePackObject]
public class NodeRef
{
    [Key(0)]
    public object type { get; set; } = "node";
    [Key(1)]
    public string id { get; set; } = "";
}
