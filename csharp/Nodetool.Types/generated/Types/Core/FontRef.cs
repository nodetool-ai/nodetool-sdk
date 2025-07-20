using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types.Core;

[MessagePackObject]
public class FontRef
{
    [Key(0)]
    public object type { get; set; } = "font";
    [Key(1)]
    public string name { get; set; } = "";
}
