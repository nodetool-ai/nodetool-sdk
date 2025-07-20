using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types;

[MessagePackObject]
public class RankingResult
{
    [Key(0)]
    public object type { get; set; } = "ranking_result";
    [Key(1)]
    public double score { get; set; }
    [Key(2)]
    public string text { get; set; }
}
