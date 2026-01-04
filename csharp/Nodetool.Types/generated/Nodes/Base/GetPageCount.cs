using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class GetPageCount
{
    [Key(0)]
    public Nodetool.Types.Core.DocumentRef pdf { get; set; } = null;

    public int Process()
    {
        return default(int);
    }
}
