using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Nodetool_fal;

[MessagePackObject]
public class F5TTS
{
    [Key(0)]
    public string gen_text { get; set; } = "";
    [Key(1)]
    public string ref_audio_url { get; set; } = "";
    [Key(2)]
    public string ref_text { get; set; } = "";
    [Key(3)]
    public string model_type { get; set; } = "F5-TTS";
    [Key(4)]
    public bool remove_silence { get; set; } = true;

    public Nodetool.Types.AudioRef Process()
    {
        // Implementation would be generated based on node logic
        return default(Nodetool.Types.AudioRef);
    }
}
