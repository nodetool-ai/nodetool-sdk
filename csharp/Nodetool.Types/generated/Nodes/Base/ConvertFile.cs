using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class ConvertFile
{
    [Key(0)]
    public object extra_args { get; set; } = new();
    [Key(1)]
    public object input_format { get; set; }
    [Key(2)]
    public Nodetool.Types.Core.FilePath input_path { get; set; } = new Nodetool.Types.Core.FilePath();
    [Key(3)]
    public object output_format { get; set; }

    public string Process()
    {
        return default(string);
    }
}
