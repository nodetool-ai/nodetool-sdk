using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class Paste
{
    [Key(0)]
    public Nodetool.Types.Core.ImageRef image { get; set; } = new Nodetool.Types.Core.ImageRef();
    [Key(1)]
    public int left { get; set; } = 0;
    [Key(2)]
    public Nodetool.Types.Core.ImageRef paste { get; set; } = new Nodetool.Types.Core.ImageRef();
    [Key(3)]
    public int top { get; set; } = 0;

    public Nodetool.Types.Core.ImageRef Process()
    {
        return default(Nodetool.Types.Core.ImageRef);
    }
}
