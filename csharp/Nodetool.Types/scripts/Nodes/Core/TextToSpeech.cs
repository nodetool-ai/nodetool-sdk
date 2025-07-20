using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Core;

[MessagePackObject]
public class TextToSpeech
{
    [Key(0)]
    public object model { get; set; } = "TtsModel.tts_1";
    [Key(1)]
    public object voice { get; set; } = "Voice.ALLOY";
    [Key(2)]
    public string input { get; set; } = "";
    [Key(3)]
    public double speed { get; set; } = 1.0;

    public Nodetool.Types.AudioRef Process()
    {
        // Implementation would be generated based on node logic
        return default(Nodetool.Types.AudioRef);
    }
}
