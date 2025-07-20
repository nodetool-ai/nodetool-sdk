using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class GetRequestDocument
{
    [Key(0)]
    public string url { get; set; } = "";

    public Nodetool.Types.DocumentRef Process()
    {
        return default(Nodetool.Types.DocumentRef);
    }
}
