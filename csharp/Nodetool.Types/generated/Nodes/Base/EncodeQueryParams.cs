using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class EncodeQueryParams
{
    [Key(0)]
    public object params { get; set; } = null;

    public string Process()
    {
        return default(string);
    }
}
