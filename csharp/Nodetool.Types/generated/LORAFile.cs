using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types;

[MessagePackObject]
public class LORAFile
{
    [Key(0)]
    public object type { get; set; } = "comfy.lora_file";
    [Key(1)]
    public string name { get; set; } = "";
}
