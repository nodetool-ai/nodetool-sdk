using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types.Core;

[MessagePackObject]
public class StyleModel
{
    [Key(0)]
    public object type { get; set; } = "comfy.style_model";
    [Key(1)]
    public string name { get; set; } = "";
    [Key(2)]
    public object model { get; set; } = null;
}
