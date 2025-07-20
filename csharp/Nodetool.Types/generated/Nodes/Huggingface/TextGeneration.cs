using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Huggingface;

[MessagePackObject]
public class TextGeneration
{
    [Key(0)]
    public Nodetool.Types.HFTextGeneration model { get; set; } = new Nodetool.Types.HFTextGeneration();
    [Key(1)]
    public string prompt { get; set; } = "";
    [Key(2)]
    public int max_new_tokens { get; set; } = 50;
    [Key(3)]
    public double temperature { get; set; } = 1.0;
    [Key(4)]
    public double top_p { get; set; } = 1.0;
    [Key(5)]
    public bool do_sample { get; set; } = true;

    public string Process()
    {
        return default(string);
    }
}
