using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types.Core;

[MessagePackObject]
public class CLIPVisionFile
{
    [Key(0)]
    public object type { get; set; } = "comfy.clip_vision_file";
    [Key(1)]
    public string name { get; set; } = "";
}
