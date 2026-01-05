using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class FilterFileNames
{
    [Key(0)]
    public bool case_sensitive { get; set; } = true;
    [Key(1)]
    public object filenames { get; set; } = new();
    [Key(2)]
    public string pattern { get; set; } = @"*";

    public object Process()
    {
        return default(object);
    }
}
