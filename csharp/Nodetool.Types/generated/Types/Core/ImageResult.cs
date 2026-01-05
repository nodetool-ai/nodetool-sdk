using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types.Core;

[MessagePackObject]
public class ImageResult
{
    [Key(0)]
    public bool is_product { get; set; }
    [Key(1)]
    public string link { get; set; }
    [Key(2)]
    public string original { get; set; }
    [Key(3)]
    public int original_height { get; set; }
    [Key(4)]
    public int original_width { get; set; }
    [Key(5)]
    public int position { get; set; }
    [Key(6)]
    public string source { get; set; }
    [Key(7)]
    public string thumbnail { get; set; }
    [Key(8)]
    public string title { get; set; }
    [Key(9)]
    public object type { get; set; } = @"image_result";
}
