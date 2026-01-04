using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class ColorInput
{
    [Key(0)]
    public string description { get; set; } = @"";
    [Key(1)]
    public string name { get; set; } = @"";
    [Key(2)]
    public Nodetool.Types.Core.ColorRef value { get; set; } = new Nodetool.Types.Core.ColorRef();

    public Nodetool.Types.Core.ColorRef Process()
    {
        return default(Nodetool.Types.Core.ColorRef);
    }
}
