using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class IntegerOutput
{
    [Key(0)]
    public int value { get; set; } = 0;
    [Key(1)]
    public string name { get; set; } = "";
    [Key(2)]
    public string description { get; set; } = "";

    public int Process()
    {
        return default(int);
    }
}
