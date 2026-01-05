using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types.Core;

[MessagePackObject]
public class CLIPVisionOutput
{
    [Key(0)]
    public object data { get; set; } = null;
    [Key(1)]
    public object type { get; set; } = @"comfy.clip_vision_output";
}
