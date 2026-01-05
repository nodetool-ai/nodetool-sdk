using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class Collect
{
    [Key(0)]
    public string input_item { get; set; } = @"";
    [Key(1)]
    public string separator { get; set; } = @"";

    public string Process()
    {
        return default(string);
    }
}
