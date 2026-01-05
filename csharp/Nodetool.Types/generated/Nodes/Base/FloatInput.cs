using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class FloatInput
{
    [Key(0)]
    public string description { get; set; } = @"";
    [Key(1)]
    public double max { get; set; } = 100;
    [Key(2)]
    public double min { get; set; } = 0;
    [Key(3)]
    public string name { get; set; } = @"";
    [Key(4)]
    public double value { get; set; } = 0.0;

    public double Process()
    {
        return default(double);
    }
}
