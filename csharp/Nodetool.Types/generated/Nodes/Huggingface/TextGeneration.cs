using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Huggingface;

[MessagePackObject]
public class TextGeneration
{
    [Key(0)]
    public bool do_sample { get; set; } = true;
    [Key(1)]
    public int max_new_tokens { get; set; } = 50;
    [Key(2)]
    public Nodetool.Types.Core.HFTextGeneration model { get; set; } = new Nodetool.Types.Core.HFTextGeneration();
    [Key(3)]
    public string prompt { get; set; } = @"";
    [Key(4)]
    public double temperature { get; set; } = 1.0;
    [Key(5)]
    public double top_p { get; set; } = 1.0;

    [MessagePackObject]
    public class TextGenerationOutput
    {
        [Key(0)]
        public Nodetool.Types.Core.Chunk chunk { get; set; }
        [Key(1)]
        public string text { get; set; }
    }

    public TextGenerationOutput Process()
    {
        return new TextGenerationOutput();
    }
}
