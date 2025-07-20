using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class FilterNone
{
    [Key(0)]
    public object values { get; set; } = new List<object>();

    public object Process()
    {
        return default(object);
    }
}
