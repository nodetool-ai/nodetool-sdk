using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class ExtractJSON
{
    [Key(0)]
    public string text { get; set; } = "";
    [Key(1)]
    public string json_path { get; set; } = "$.*";
    [Key(2)]
    public bool find_all { get; set; } = false;

    public object Process()
    {
        return default(object);
    }
}
