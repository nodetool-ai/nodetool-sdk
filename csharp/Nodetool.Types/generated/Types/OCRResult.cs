using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types;

[MessagePackObject]
public class OCRResult
{
    [Key(0)]
    public object type { get; set; } = "ocr_result";
    [Key(1)]
    public string text { get; set; }
    [Key(2)]
    public double score { get; set; }
    [Key(3)]
    public List<int> top_left { get; set; }
    [Key(4)]
    public List<int> top_right { get; set; }
    [Key(5)]
    public List<int> bottom_right { get; set; }
    [Key(6)]
    public List<int> bottom_left { get; set; }
}
