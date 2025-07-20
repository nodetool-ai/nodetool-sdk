using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types;

[MessagePackObject]
public class Task
{
    [Key(0)]
    public object type { get; set; } = "task";
    [Key(1)]
    public string id { get; set; } = "";
    [Key(2)]
    public string title { get; set; } = "";
    [Key(3)]
    public string description { get; set; } = "";
    [Key(4)]
    public List<Nodetool.Types.SubTask> subtasks { get; set; } = new List<object>();
}
