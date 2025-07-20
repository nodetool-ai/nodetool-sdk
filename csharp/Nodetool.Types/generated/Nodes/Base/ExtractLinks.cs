using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class ExtractLinks
{
    [Key(0)]
    public string markdown { get; set; } = "";
    [Key(1)]
    public bool include_titles { get; set; } = true;

    public object Process()
    {
        return default(object);
    }
}
