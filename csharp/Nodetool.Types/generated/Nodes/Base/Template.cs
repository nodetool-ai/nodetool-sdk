using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class Template
{
    [Key(0)]
    public string string { get; set; } = "";
    [Key(1)]
    public object values { get; set; } = new Dictionary<string, object>();

    public string Process()
    {
        return default(string);
    }
}
