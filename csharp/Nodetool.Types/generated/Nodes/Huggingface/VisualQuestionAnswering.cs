using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Huggingface;

[MessagePackObject]
public class VisualQuestionAnswering
{
    [Key(0)]
    public Nodetool.Types.HFVisualQuestionAnswering model { get; set; } = new Nodetool.Types.HFVisualQuestionAnswering();
    [Key(1)]
    public Nodetool.Types.ImageRef image { get; set; } = new Nodetool.Types.ImageRef();
    [Key(2)]
    public string question { get; set; } = "";

    public string Process()
    {
        return default(string);
    }
}
