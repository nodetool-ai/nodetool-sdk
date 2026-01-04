using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class GetJSONPathStr
{
    [Key(0)]
    public object data { get; set; } = null;
    [Key(1)]
    public string default_ { get; set; } = @"";
    [Key(2)]
    public string path { get; set; } = @"";

    public string Process()
    {
        return default(string);
    }
}
