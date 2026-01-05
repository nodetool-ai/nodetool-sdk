using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types.Core;

[MessagePackObject]
public class Message
{
    [Key(0)]
    public object agent_mode { get; set; } = null;
    [Key(1)]
    public object collections { get; set; } = null;
    [Key(2)]
    public object content { get; set; } = null;
    [Key(3)]
    public object created_at { get; set; } = null;
    [Key(4)]
    public object error_type { get; set; } = null;
    [Key(5)]
    public object graph { get; set; } = null;
    [Key(6)]
    public object help_mode { get; set; } = null;
    [Key(7)]
    public object id { get; set; } = null;
    [Key(8)]
    public object input_files { get; set; } = null;
    [Key(9)]
    public object model { get; set; } = null;
    [Key(10)]
    public object name { get; set; } = null;
    [Key(11)]
    public object output_files { get; set; } = null;
    [Key(12)]
    public object provider { get; set; } = null;
    [Key(13)]
    public string role { get; set; } = @"";
    [Key(14)]
    public object thread_id { get; set; } = null;
    [Key(15)]
    public object tool_call_id { get; set; } = null;
    [Key(16)]
    public object tool_calls { get; set; } = null;
    [Key(17)]
    public object tools { get; set; } = null;
    [Key(18)]
    public object type { get; set; } = @"message";
    [Key(19)]
    public object workflow_assistant { get; set; } = null;
    [Key(20)]
    public object workflow_id { get; set; } = null;
}
