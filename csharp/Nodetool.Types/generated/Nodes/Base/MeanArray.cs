using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class MeanArray
{
    [Key(0)]
    public int? axis { get; set; } = null;
    [Key(1)]
    public Nodetool.Types.Core.NPArray values { get; set; } = new Nodetool.Types.Core.NPArray();

    public object Process()
    {
        return default(object);
    }
}
