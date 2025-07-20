using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types;

[MessagePackObject]
public class Collection
{
    [Key(0)]
    public object type { get; set; } = "collection";
    [Key(1)]
    public string name { get; set; } = "";
}
