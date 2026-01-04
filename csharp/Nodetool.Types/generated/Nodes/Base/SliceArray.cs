using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class SliceArray
{
    [Key(0)]
    public int axis { get; set; } = 0;
    [Key(1)]
    public int start { get; set; } = 0;
    [Key(2)]
    public int step { get; set; } = 1;
    [Key(3)]
    public int stop { get; set; } = 0;
    [Key(4)]
    public Nodetool.Types.Core.NPArray values { get; set; } = new Nodetool.Types.Core.NPArray();

    public Nodetool.Types.Core.NPArray Process()
    {
        return default(Nodetool.Types.Core.NPArray);
    }
}
