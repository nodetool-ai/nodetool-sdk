using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types;

[MessagePackObject]
public class IPAdapterFile
{
    [Key(0)]
    public object type { get; set; } = "comfy.ip_adapter_file";
    [Key(1)]
    public string name { get; set; } = "";
}
