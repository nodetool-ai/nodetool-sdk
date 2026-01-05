using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class GetJSONPathBool
{
    [Key(0)]
    public object data { get; set; } = null;
    [Key(1)]
    public bool default_ { get; set; } = false;
    [Key(2)]
    public string path { get; set; } = @"";

    public bool Process()
    {
        return default(bool);
    }
}
