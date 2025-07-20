using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class SimpleAgentNode
{
    [Key(0)]
    public string name { get; set; } = "Simple Agent";
    [Key(1)]
    public string objective { get; set; } = "";
    [Key(2)]
    public Nodetool.Types.LanguageModel model { get; set; } = new Nodetool.Types.LanguageModel();
    [Key(3)]
    public object tools { get; set; } = new List<object>();
    [Key(4)]
    public object output_schema { get; set; } = null;
    [Key(5)]
    public int max_iterations { get; set; } = 20;

    public string Process()
    {
        return default(string);
    }
}
