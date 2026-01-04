using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class GetJSONPathList
{
    [Key(0)]
    public object data { get; set; } = null;
    [Key(1)]
    public object default_ { get; set; } = new();
    [Key(2)]
    public string path { get; set; } = @"";

    public object Process()
    {
        return default(object);
    }
}
