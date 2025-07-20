using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types;

[MessagePackObject]
public class NPArray
{
    [Key(0)]
    public object type { get; set; } = "np_array";
    [Key(1)]
    public object value { get; set; } = null;
    [Key(2)]
    public string dtype { get; set; } = "<i8";
    [Key(3)]
    public List<int> shape { get; set; }
}
