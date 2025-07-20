using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Nodetool_huggingface;

[MessagePackObject]
public class Whisper
{
    [Key(0)]
    public Nodetool.Types.HFAutomaticSpeechRecognition model { get; set; } = new Nodetool.Types.HFAutomaticSpeechRecognition();
    [Key(1)]
    public Nodetool.Types.AudioRef audio { get; set; } = new Nodetool.Types.AudioRef();
    [Key(2)]
    public object task { get; set; } = "Task.TRANSCRIBE";
    [Key(3)]
    public object language { get; set; } = "WhisperLanguage.NONE";
    [Key(4)]
    public object timestamps { get; set; } = "Timestamps.NONE";

    [MessagePackObject]
    public class WhisperOutput
    {
        [Key(0)]
        public string text { get; set; }
        [Key(1)]
        public object chunks { get; set; }
    }

    public WhisperOutput Process()
    {
        // Implementation would be generated based on node logic
        return new WhisperOutput();
    }
}
