using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class Schema
{
    [Key(0)]
    public Nodetool.Types.Core.RecordType columns { get; set; } = new Nodetool.Types.Core.RecordType();

    public Nodetool.Types.Core.RecordType Process()
    {
        return default(Nodetool.Types.Core.RecordType);
    }
}
