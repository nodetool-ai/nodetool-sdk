using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class ExtractNumberedLists
{
    [Key(0)]
    public string markdown { get; set; } = "";

    public object Process()
    {
        return default(object);
    }
}
