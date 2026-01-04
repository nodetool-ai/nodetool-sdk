using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Fal;

[MessagePackObject]
public class Whisper
{
    [Key(0)]
    public Nodetool.Types.Core.AudioRef audio { get; set; } = new Nodetool.Types.Core.AudioRef();
    [Key(1)]
    public int batch_size { get; set; } = 64;
    [Key(2)]
    public object chunk_level { get; set; } = @"segment";
    [Key(3)]
    public bool diarize { get; set; } = false;
    [Key(4)]
    public object language { get; set; } = @"en";
    [Key(5)]
    public int num_speakers { get; set; } = 1;
    [Key(6)]
    public string prompt { get; set; } = @"";
    [Key(7)]
    public object task { get; set; } = @"transcribe";

    [MessagePackObject]
    public class WhisperOutput
    {
        [Key(0)]
        public object chunks { get; set; }
        [Key(1)]
        public object diarization_segments { get; set; }
        [Key(2)]
        public object inferred_languages { get; set; }
        [Key(3)]
        public string text { get; set; }
    }

    public WhisperOutput Process()
    {
        return new WhisperOutput();
    }
}
