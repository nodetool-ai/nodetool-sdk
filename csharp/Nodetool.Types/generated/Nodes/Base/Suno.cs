using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class Suno
{
    [Key(0)]
    public int duration { get; set; } = 60;
    [Key(1)]
    public bool instrumental { get; set; } = false;
    [Key(2)]
    public string lyrics { get; set; } = @"";
    [Key(3)]
    public object model { get; set; } = @"v4.5+";
    [Key(4)]
    public string prompt { get; set; } = @"";
    [Key(5)]
    public object style { get; set; } = @"custom";

    public Nodetool.Types.Core.AudioRef Process()
    {
        return default(Nodetool.Types.Core.AudioRef);
    }
}
