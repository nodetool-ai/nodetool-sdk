using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Huggingface;

[MessagePackObject]
public class TextToText
{
    [Key(0)]
    public Nodetool.Types.HFText2TextGeneration model { get; set; } = new Nodetool.Types.HFText2TextGeneration();
    [Key(1)]
    public string text { get; set; } = "";
    [Key(2)]
    public int max_length { get; set; } = 50;

    public string Process()
    {
        return default(string);
    }
}
