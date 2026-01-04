using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types.Core;

[MessagePackObject]
public class NewsResult
{
    [Key(0)]
    public string date { get; set; }
    [Key(1)]
    public string link { get; set; }
    [Key(2)]
    public int position { get; set; }
    [Key(3)]
    public object thumbnail { get; set; } = null;
    [Key(4)]
    public object title { get; set; } = null;
    [Key(5)]
    public object type { get; set; } = @"news_result";
}
