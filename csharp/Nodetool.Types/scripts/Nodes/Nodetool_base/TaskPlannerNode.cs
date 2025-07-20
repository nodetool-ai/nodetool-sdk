using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Nodetool_base;

[MessagePackObject]
public class TaskPlannerNode
{
    [Key(0)]
    public string name { get; set; } = "Task Planner";
    [Key(1)]
    public string objective { get; set; } = "";
    [Key(2)]
    public Nodetool.Types.LanguageModel model { get; set; } = new Nodetool.Types.LanguageModel();
    [Key(3)]
    public Nodetool.Types.LanguageModel reasoning_model { get; set; } = new Nodetool.Types.LanguageModel();
    [Key(4)]
    public object tools { get; set; } = new List<object>();
    [Key(5)]
    public object output_schema { get; set; } = null;
    [Key(6)]
    public bool enable_analysis_phase { get; set; } = true;
    [Key(7)]
    public bool enable_data_contracts_phase { get; set; } = true;
    [Key(8)]
    public bool use_structured_output { get; set; } = false;

    public Nodetool.Types.Task Process()
    {
        // Implementation would be generated based on node logic
        return default(Nodetool.Types.Task);
    }
}
