using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class ExtractLinks
{
    [Key(0)]
    public bool include_titles { get; set; } = true;
    [Key(1)]
    public string markdown { get; set; } = @"";

    public object Process()
    {
        return default(object);
    }
}
