using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class ParseDate
{
    [Key(0)]
    public string date_string { get; set; } = @"";
    [Key(1)]
    public object input_format { get; set; } = @"%Y-%m-%d";

    public Nodetool.Types.Core.Date Process()
    {
        return default(Nodetool.Types.Core.Date);
    }
}
