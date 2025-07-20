using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Fal;

[MessagePackObject]
public class Whisper
{
    [Key(0)]
    public Nodetool.Types.AudioRef audio { get; set; } = new Nodetool.Types.AudioRef();
    [Key(1)]
    public object task { get; set; } = "TaskEnum.TRANSCRIBE";
    [Key(2)]
    public object language { get; set; } = "LanguageEnum.EN";
    [Key(3)]
    public bool diarize { get; set; } = false;
    [Key(4)]
    public object chunk_level { get; set; } = "ChunkLevelEnum.SEGMENT";
    [Key(5)]
    public int num_speakers { get; set; } = 1;
    [Key(6)]
    public int batch_size { get; set; } = 64;
    [Key(7)]
    public string prompt { get; set; } = "";

    [MessagePackObject]
    public class WhisperOutput
    {
        [Key(0)]
        public string text { get; set; }
        [Key(1)]
        public object chunks { get; set; }
        [Key(2)]
        public object inferred_languages { get; set; }
        [Key(3)]
        public object diarization_segments { get; set; }
    }

    public WhisperOutput Process()
    {
        // Implementation would be generated based on node logic
        return new WhisperOutput();
    }
}
