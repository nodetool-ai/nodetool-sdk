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
    public string path { get; set; } = "";
    [Key(2)]
    public bool default { get; set; } = false;

    public bool Process()
    {
        return default(bool);
    }
}
