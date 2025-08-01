using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types.Core;

[MessagePackObject]
public class CLIPFile
{
    [Key(0)]
    public object type { get; set; } = "comfy.clip_file";
    [Key(1)]
    public string name { get; set; } = "";
}
