using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types.Core;

[MessagePackObject]
public class TextChunk
{
    [Key(0)]
    public object type { get; set; } = "text_chunk";
    [Key(1)]
    public string text { get; set; } = "";
    [Key(2)]
    public string source_id { get; set; } = "";
    [Key(3)]
    public int start_index { get; set; } = 0;
}
