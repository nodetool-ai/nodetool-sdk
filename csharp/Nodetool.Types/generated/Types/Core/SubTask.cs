using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types.Core;

[MessagePackObject]
public class SubTask
{
    [Key(0)]
    public bool completed { get; set; } = false;
    [Key(1)]
    public string content { get; set; }
    [Key(2)]
    public int end_time { get; set; } = 0;
    [Key(3)]
    public string id { get; set; } = @"";
    [Key(4)]
    public List<string> input_files { get; set; } = new();
    [Key(5)]
    public List<string> input_tasks { get; set; } = new();
    [Key(6)]
    public string item_output_schema { get; set; } = @"";
    [Key(7)]
    public string item_template { get; set; } = @"";
    [Key(8)]
    public List<Nodetool.Types.Core.LogEntry> logs { get; set; } = new();
    [Key(9)]
    public object? mode { get; set; } = null;
    [Key(10)]
    public string output_file { get; set; } = @"";
    [Key(11)]
    public string output_schema { get; set; } = @"";
    [Key(12)]
    public int start_time { get; set; } = 0;
    [Key(13)]
    public List<string> tools { get; set; } = new();
    [Key(14)]
    public object type { get; set; } = @"subtask";
}
