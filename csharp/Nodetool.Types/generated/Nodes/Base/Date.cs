using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class Date
{
    [Key(0)]
    public int day { get; set; } = 1;
    [Key(1)]
    public int month { get; set; } = 1;
    [Key(2)]
    public int year { get; set; } = 1900;

    public Nodetool.Types.Core.Date Process()
    {
        return default(Nodetool.Types.Core.Date);
    }
}
