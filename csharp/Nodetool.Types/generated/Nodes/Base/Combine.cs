using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class Combine
{
    [Key(0)]
    public object dict_a { get; set; } = new();
    [Key(1)]
    public object dict_b { get; set; } = new();

    public object Process()
    {
        return default(object);
    }
}
