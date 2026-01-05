using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types.Core;

[MessagePackObject]
public class AudioChunk
{
    [Key(0)]
    public string text { get; set; } = @"";
    [Key(1)]
    public List<double> timestamp { get; set; }
    [Key(2)]
    public object type { get; set; } = @"audio_chunk";
}
