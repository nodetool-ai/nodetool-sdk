using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Core;

[MessagePackObject]
public class Transcribe
{
    [Key(0)]
    public object model { get; set; } = "TranscriptionModel.WHISPER_1";
    [Key(1)]
    public Nodetool.Types.AudioRef audio { get; set; } = new Nodetool.Types.AudioRef();
    [Key(2)]
    public object language { get; set; } = "Language.NONE";
    [Key(3)]
    public bool timestamps { get; set; } = false;
    [Key(4)]
    public string prompt { get; set; } = "";
    [Key(5)]
    public double temperature { get; set; } = 0;

    [MessagePackObject]
    public class TranscribeOutput
    {
        [Key(0)]
        public string text { get; set; }
        [Key(1)]
        public object words { get; set; }
        [Key(2)]
        public object segments { get; set; }
    }

    public TranscribeOutput Process()
    {
        // Implementation would be generated based on node logic
        return new TranscribeOutput();
    }
}
