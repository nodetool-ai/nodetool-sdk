using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types.Core;

[MessagePackObject]
public class SubTask
{
    [Key(0)]
    public object type { get; set; } = "subtask";
    [Key(1)]
    public string id { get; set; } = "";
    [Key(2)]
    public object model { get; set; } = null;
    [Key(3)]
    public string content { get; set; }
    [Key(4)]
    public List<Nodetool.Types.LogEntry> logs { get; set; } = new List<object>();
    [Key(5)]
    public int max_iterations { get; set; } = 10;
    [Key(6)]
    public int max_tool_calls { get; set; } = 10;
    [Key(7)]
    public bool completed { get; set; } = false;
    [Key(8)]
    public int start_time { get; set; } = 0;
    [Key(9)]
    public int end_time { get; set; } = 0;
    [Key(10)]
    public List<string> input_tasks { get; set; } = new List<object>();
    [Key(11)]
    public List<string> input_files { get; set; } = new List<object>();
    [Key(12)]
    public string output_file { get; set; } = "";
    [Key(13)]
    public string output_schema { get; set; } = "";
    [Key(14)]
    public bool is_intermediate_result { get; set; } = false;
}
