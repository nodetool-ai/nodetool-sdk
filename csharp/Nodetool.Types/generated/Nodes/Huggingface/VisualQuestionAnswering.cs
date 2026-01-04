using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Huggingface;

[MessagePackObject]
public class VisualQuestionAnswering
{
    [Key(0)]
    public Nodetool.Types.Core.ImageRef image { get; set; } = new Nodetool.Types.Core.ImageRef();
    [Key(1)]
    public Nodetool.Types.Core.HFVisualQuestionAnswering model { get; set; } = new Nodetool.Types.Core.HFVisualQuestionAnswering();
    [Key(2)]
    public string question { get; set; } = @"";

    public string Process()
    {
        return default(string);
    }
}
