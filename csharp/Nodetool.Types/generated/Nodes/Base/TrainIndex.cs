using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class TrainIndex
{
    [Key(0)]
    public Nodetool.Types.Core.FaissIndex index { get; set; } = new Nodetool.Types.Core.FaissIndex();
    [Key(1)]
    public Nodetool.Types.Core.NPArray vectors { get; set; } = new Nodetool.Types.Core.NPArray();

    public Nodetool.Types.Core.FaissIndex Process()
    {
        return default(Nodetool.Types.Core.FaissIndex);
    }
}
