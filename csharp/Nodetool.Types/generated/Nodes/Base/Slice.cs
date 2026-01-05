using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class Slice
{
    [Key(0)]
    public int? start { get; set; } = null;
    [Key(1)]
    public int? step { get; set; } = null;
    [Key(2)]
    public int? stop { get; set; } = null;
    [Key(3)]
    public string text { get; set; } = @"";

    public string Process()
    {
        return default(string);
    }
}
