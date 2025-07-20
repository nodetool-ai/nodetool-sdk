using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types;

[MessagePackObject]
public class AudioChunk
{
    [Key(0)]
    public object type { get; set; } = "audio_chunk";
    [Key(1)]
    public List<double> timestamp { get; set; }
    [Key(2)]
    public string text { get; set; } = "";
}
