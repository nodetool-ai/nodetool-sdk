using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class TextToSpeech
{
    [Key(0)]
    public string input { get; set; } = @"";
    [Key(1)]
    public object model { get; set; } = @"tts-1";
    [Key(2)]
    public double speed { get; set; } = 1.0;
    [Key(3)]
    public object voice { get; set; } = @"alloy";

    public Nodetool.Types.Core.AudioRef Process()
    {
        return default(Nodetool.Types.Core.AudioRef);
    }
}
