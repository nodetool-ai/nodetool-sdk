using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types.Core;

[MessagePackObject]
public class Source
{
    [Key(0)]
    public string title { get; set; } = @"";
    [Key(1)]
    public object type { get; set; } = @"source";
    [Key(2)]
    public string url { get; set; } = @"";
}
