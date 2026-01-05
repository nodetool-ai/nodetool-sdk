using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class RealtimeTranscription
{
    [Key(0)]
    public Nodetool.Types.Core.LanguageModel model { get; set; } = new Nodetool.Types.Core.LanguageModel();
    [Key(1)]
    public string system { get; set; } = @"";
    [Key(2)]
    public double temperature { get; set; } = 0.8;

    [MessagePackObject]
    public class RealtimeTranscriptionOutput
    {
        [Key(0)]
        public Nodetool.Types.Core.Chunk chunk { get; set; }
        [Key(1)]
        public string text { get; set; }
    }

    public RealtimeTranscriptionOutput Process()
    {
        return new RealtimeTranscriptionOutput();
    }
}
