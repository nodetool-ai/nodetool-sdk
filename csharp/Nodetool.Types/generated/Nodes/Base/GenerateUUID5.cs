using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class GenerateUUID5
{
    [Key(0)]
    public string name { get; set; } = @"";
    [Key(1)]
    public string namespace_ { get; set; } = @"dns";

    public string Process()
    {
        return default(string);
    }
}
