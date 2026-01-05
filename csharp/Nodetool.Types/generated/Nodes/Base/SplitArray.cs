using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class SplitArray
{
    [Key(0)]
    public int axis { get; set; } = 0;
    [Key(1)]
    public int num_splits { get; set; } = 0;
    [Key(2)]
    public Nodetool.Types.Core.NPArray values { get; set; } = new Nodetool.Types.Core.NPArray();

    public object Process()
    {
        return default(object);
    }
}
