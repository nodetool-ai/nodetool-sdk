using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types.Core;

[MessagePackObject]
public class OrganicResult
{
    [Key(0)]
    public string? date { get; set; } = null;
    [Key(1)]
    public string displayed_link { get; set; }
    [Key(2)]
    public string link { get; set; }
    [Key(3)]
    public int position { get; set; }
    [Key(4)]
    public string? redirect_link { get; set; } = null;
    [Key(5)]
    public string snippet { get; set; }
    [Key(6)]
    public List<string>? snippet_highlighted_words { get; set; } = null;
    [Key(7)]
    public string? thumbnail { get; set; } = null;
    [Key(8)]
    public string title { get; set; }
    [Key(9)]
    public object type { get; set; } = @"organic_result";
}
