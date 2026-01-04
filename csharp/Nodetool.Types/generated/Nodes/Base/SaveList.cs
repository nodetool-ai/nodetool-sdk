using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class SaveList
{
    [Key(0)]
    public string name { get; set; } = @"text.txt";
    [Key(1)]
    public object values { get; set; } = new();

    public Nodetool.Types.Core.TextRef Process()
    {
        return default(Nodetool.Types.Core.TextRef);
    }
}
