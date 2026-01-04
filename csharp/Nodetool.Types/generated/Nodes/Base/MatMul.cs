using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class MatMul
{
    [Key(0)]
    public Nodetool.Types.Core.NPArray a { get; set; } = new Nodetool.Types.Core.NPArray();
    [Key(1)]
    public Nodetool.Types.Core.NPArray b { get; set; } = new Nodetool.Types.Core.NPArray();

    public Nodetool.Types.Core.NPArray Process()
    {
        return default(Nodetool.Types.Core.NPArray);
    }
}
