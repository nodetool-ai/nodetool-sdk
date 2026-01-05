using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class AddLabel
{
    [Key(0)]
    public string label { get; set; } = @"";
    [Key(1)]
    public string message_id { get; set; } = @"";

    public bool Process()
    {
        return default(bool);
    }
}
