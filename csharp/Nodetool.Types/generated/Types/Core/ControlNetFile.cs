using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types.Core;

[MessagePackObject]
public class ControlNetFile
{
    [Key(0)]
    public object type { get; set; } = "comfy.control_net_file";
    [Key(1)]
    public string name { get; set; } = "";
}
