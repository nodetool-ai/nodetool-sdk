using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class Slice
{
    [Key(0)]
    public string text { get; set; } = "";
    [Key(1)]
    public object start { get; set; } = null;
    [Key(2)]
    public object stop { get; set; } = null;
    [Key(3)]
    public object step { get; set; } = null;

    public string Process()
    {
        return default(string);
    }
}
