using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class GetJSONPathInt
{
    [Key(0)]
    public object data { get; set; } = null;
    [Key(1)]
    public int default_ { get; set; } = 0;
    [Key(2)]
    public string path { get; set; } = @"";

    public int Process()
    {
        return default(int);
    }
}
