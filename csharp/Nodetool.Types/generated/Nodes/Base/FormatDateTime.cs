using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class FormatDateTime
{
    [Key(0)]
    public Nodetool.Types.Datetime input_datetime { get; set; } = new Nodetool.Types.Datetime();
    [Key(1)]
    public object output_format { get; set; } = "DateFormat.HUMAN_READABLE";

    public string Process()
    {
        return default(string);
    }
}
