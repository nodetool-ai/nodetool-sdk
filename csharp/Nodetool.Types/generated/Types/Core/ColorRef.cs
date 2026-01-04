using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types.Core;

[MessagePackObject]
public class ColorRef
{
    [Key(0)]
    public object type { get; set; } = @"color";
    [Key(1)]
    public object value { get; set; } = null;
}
