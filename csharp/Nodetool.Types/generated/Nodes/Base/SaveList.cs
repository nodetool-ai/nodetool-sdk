using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class SaveList
{
    [Key(0)]
    public object values { get; set; } = new List<object>();
    [Key(1)]
    public string name { get; set; } = "text.txt";

    public Nodetool.Types.TextRef Process()
    {
        return default(Nodetool.Types.TextRef);
    }
}
