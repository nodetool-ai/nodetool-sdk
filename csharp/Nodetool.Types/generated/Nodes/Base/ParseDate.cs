using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class ParseDate
{
    [Key(0)]
    public string date_string { get; set; } = "";
    [Key(1)]
    public object input_format { get; set; } = "DateFormat.ISO";

    public Nodetool.Types.Date Process()
    {
        return default(Nodetool.Types.Date);
    }
}
