using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class Transcribe
{
    [Key(0)]
    public Nodetool.Types.Core.AudioRef audio { get; set; } = new Nodetool.Types.Core.AudioRef();
    [Key(1)]
    public object language { get; set; } = @"auto_detect";
    [Key(2)]
    public object model { get; set; } = @"whisper-1";
    [Key(3)]
    public string prompt { get; set; } = @"";
    [Key(4)]
    public double temperature { get; set; } = 0;
    [Key(5)]
    public bool timestamps { get; set; } = false;

    [MessagePackObject]
    public class TranscribeOutput
    {
        [Key(0)]
        public object segments { get; set; }
        [Key(1)]
        public string text { get; set; }
        [Key(2)]
        public object words { get; set; }
    }

    public TranscribeOutput Process()
    {
        return new TranscribeOutput();
    }
}
