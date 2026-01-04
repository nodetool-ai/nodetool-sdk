using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class Zip
{
    [Key(0)]
    public object keys { get; set; } = new();
    [Key(1)]
    public object values { get; set; } = new();

    public object Process()
    {
        return default(object);
    }
}
