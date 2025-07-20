using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class FilterDicts
{
    [Key(0)]
    public object values { get; set; } = new List<object>();
    [Key(1)]
    public string condition { get; set; } = "";

    public object Process()
    {
        return default(object);
    }
}
