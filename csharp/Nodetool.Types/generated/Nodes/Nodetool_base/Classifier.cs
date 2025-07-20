using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Nodetool_base;

[MessagePackObject]
public class Classifier
{
    [Key(0)]
    public string system_prompt { get; set; } = "
        You are a precise text classifier. Your task is to analyze the input text and assign confidence scores.
        ";
    [Key(1)]
    public Nodetool.Types.LanguageModel model { get; set; } = new Nodetool.Types.LanguageModel();
    [Key(2)]
    public string text { get; set; } = "";
    [Key(3)]
    public object categories { get; set; } = new List<object>();
    [Key(4)]
    public int context_window { get; set; } = 4096;

    public string Process()
    {
        // Implementation would be generated based on node logic
        return default(string);
    }
}
