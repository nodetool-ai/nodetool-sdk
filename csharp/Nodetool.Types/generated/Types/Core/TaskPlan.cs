using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types.Core;

[MessagePackObject]
public class TaskPlan
{
    [Key(0)]
    public List<Nodetool.Types.Core.Task> tasks { get; set; } = new();
    [Key(1)]
    public string title { get; set; } = @"";
    [Key(2)]
    public object type { get; set; } = @"task_plan";
}
