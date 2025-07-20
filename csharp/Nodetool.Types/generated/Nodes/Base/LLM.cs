using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class LLM
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
    public object voice { get; set; } = "Voice.NONE";
    [Key(6)]
    public object tools { get; set; } = new List<object>();
    [Key(7)]
    public object messages { get; set; } = new List<object>();
    [Key(8)]
    public int max_tokens { get; set; } = 4096;
    [Key(9)]
    public int context_window { get; set; } = 4096;

    [MessagePackObject]
    public class LLMOutput
    {
        [Key(0)]
        public string text { get; set; }
        [Key(1)]
        public Nodetool.Types.AudioRef audio { get; set; }
        [Key(2)]
        public Nodetool.Types.ImageRef image { get; set; }
    }

    public LLMOutput Process()
    {
        return new LLMOutput();
    }
}
