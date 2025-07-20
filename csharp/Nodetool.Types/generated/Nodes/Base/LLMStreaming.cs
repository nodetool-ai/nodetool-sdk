using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class LLMStreaming
{
    [Key(0)]
    public Nodetool.Types.LanguageModel model { get; set; } = new Nodetool.Types.LanguageModel();
    [Key(1)]
    public string system { get; set; } = "You are a friendly assistant.";
    [Key(2)]
    public string prompt { get; set; } = "";
    [Key(3)]
    public Nodetool.Types.ImageRef image { get; set; } = new Nodetool.Types.ImageRef();
    [Key(4)]
    public Nodetool.Types.AudioRef audio { get; set; } = new Nodetool.Types.AudioRef();
    [Key(5)]
    public object tools { get; set; } = new List<object>();
    [Key(6)]
    public object messages { get; set; } = new List<object>();
    [Key(7)]
    public int max_tokens { get; set; } = 4096;
    [Key(8)]
    public int context_window { get; set; } = 4096;

    [MessagePackObject]
    public class LLMStreamingOutput
    {
        [Key(0)]
        public string text { get; set; }
        [Key(1)]
        public Nodetool.Types.ImageRef image { get; set; }
        [Key(2)]
        public Nodetool.Types.AudioRef audio { get; set; }
    }

    public LLMStreamingOutput Process()
    {
        return new LLMStreamingOutput();
    }
}
