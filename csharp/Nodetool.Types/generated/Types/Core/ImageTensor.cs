using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types.Core;

[MessagePackObject]
public class ImageTensor
{
    [Key(0)]
    public object type { get; set; } = "comfy.image_tensor";
    [Key(1)]
    public object data { get; set; } = null;
}
