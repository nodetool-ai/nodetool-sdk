using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class ConvertText
{
    [Key(0)]
    public string content { get; set; } = null;
    [Key(1)]
    public object extra_args { get; set; } = new();
    [Key(2)]
    public object input_format { get; set; } = null;
    [Key(3)]
    public object output_format { get; set; } = null;

    public string Process()
    {
        return default(string);
    }
}
