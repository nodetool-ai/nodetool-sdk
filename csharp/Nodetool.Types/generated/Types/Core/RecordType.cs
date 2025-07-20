using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types.Core;

[MessagePackObject]
public class RecordType
{
    [Key(0)]
    public object type { get; set; } = "record_type";
    [Key(1)]
    public List<object> columns { get; set; } = new List<object>();
}
