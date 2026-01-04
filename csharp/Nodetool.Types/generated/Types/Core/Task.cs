using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types.Core;

[MessagePackObject]
public class Task
{
    [Key(0)]
    public string description { get; set; } = @"";
    [Key(1)]
    public string id { get; set; } = @"";
    [Key(2)]
    public List<Nodetool.Types.Core.SubTask> subtasks { get; set; } = new();
    [Key(3)]
    public string title { get; set; } = @"";
    [Key(4)]
    public object type { get; set; } = @"task";
}
