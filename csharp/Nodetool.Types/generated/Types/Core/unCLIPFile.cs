using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types.Core;

[MessagePackObject]
public class unCLIPFile
{
    [Key(0)]
    public object type { get; set; } = "comfy.unclip_file";
    [Key(1)]
    public string name { get; set; } = "";
}
