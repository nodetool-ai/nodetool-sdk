using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class ParseDateTime
{
    [Key(0)]
    public string datetime_string { get; set; } = "";
    [Key(1)]
    public object input_format { get; set; } = "DateFormat.ISO";

    public Nodetool.Types.Datetime Process()
    {
        return default(Nodetool.Types.Datetime);
    }
}
