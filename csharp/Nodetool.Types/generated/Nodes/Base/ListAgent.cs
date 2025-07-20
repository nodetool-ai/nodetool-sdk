using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Agents;

[MessagePackObject]
public class ListAgent
{
    [Key(0)]
    public string name { get; set; } = "Agent";
    [Key(1)]
    public string objective { get; set; } = "";
    [Key(2)]
    public Nodetool.Types.LanguageModel model { get; set; } = new Nodetool.Types.LanguageModel();
    [Key(3)]
    public Nodetool.Types.LanguageModel reasoning_model { get; set; } = new Nodetool.Types.LanguageModel();
    [Key(4)]
    public Nodetool.Types.Task task { get; set; } = new Nodetool.Types.Task();
    [Key(5)]
    public object tools { get; set; } = new List<object>();
    [Key(6)]
    public object input_files { get; set; } = new List<object>();
    [Key(7)]
    public int max_steps { get; set; } = 30;
    [Key(8)]
    public string item_type { get; set; } = "string";

    public object Process()
    {
        // Implementation would be generated based on node logic
        return default(object);
    }
}
