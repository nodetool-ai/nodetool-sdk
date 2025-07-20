using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class FloatOutput
{
    [Key(0)]
    public double value { get; set; } = 0;
    [Key(1)]
    public string name { get; set; } = "";
    [Key(2)]
    public string description { get; set; } = "";

    public double Process()
    {
        return default(double);
    }
}
