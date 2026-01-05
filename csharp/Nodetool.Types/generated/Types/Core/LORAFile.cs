using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types.Core;

[MessagePackObject]
public class LORAFile
{
    [Key(0)]
    public string name { get; set; } = @"";
    [Key(1)]
    public object type { get; set; } = @"comfy.lora_file";
}
