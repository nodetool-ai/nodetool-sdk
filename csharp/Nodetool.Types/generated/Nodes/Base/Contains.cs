using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class Contains
{
    [Key(0)]
    public bool case_sensitive { get; set; } = true;
    [Key(1)]
    public string substring { get; set; } = @"";
    [Key(2)]
    public string text { get; set; } = @"";

    public bool Process()
    {
        return default(bool);
    }
}
