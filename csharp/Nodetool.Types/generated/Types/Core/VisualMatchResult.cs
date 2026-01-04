using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types.Core;

[MessagePackObject]
public class VisualMatchResult
{
    [Key(0)]
    public object image { get; set; } = null;
    [Key(1)]
    public object image_height { get; set; } = null;
    [Key(2)]
    public object image_width { get; set; } = null;
    [Key(3)]
    public object link { get; set; } = null;
    [Key(4)]
    public int position { get; set; }
    [Key(5)]
    public object thumbnail { get; set; } = null;
    [Key(6)]
    public object thumbnail_height { get; set; } = null;
    [Key(7)]
    public object thumbnail_width { get; set; } = null;
    [Key(8)]
    public object title { get; set; } = null;
    [Key(9)]
    public object type { get; set; } = @"visual_match_result";
}
