using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

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
        return default(Nodetool.Types.AudioRef);
    }
}
