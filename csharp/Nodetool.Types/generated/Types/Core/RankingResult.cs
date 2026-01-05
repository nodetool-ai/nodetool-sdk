using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types.Core;

[MessagePackObject]
public class RankingResult
{
    [Key(0)]
    public double score { get; set; }
    [Key(1)]
    public string text { get; set; }
    [Key(2)]
    public object type { get; set; } = @"ranking_result";
}
