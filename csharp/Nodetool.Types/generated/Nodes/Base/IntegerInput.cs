using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class IntegerInput
{
    [Key(0)]
    public string description { get; set; } = @"";
    [Key(1)]
    public int max { get; set; } = 100;
    [Key(2)]
    public int min { get; set; } = 0;
    [Key(3)]
    public string name { get; set; } = @"";
    [Key(4)]
    public int value { get; set; } = 0;

    public int Process()
    {
        return default(int);
    }
}
