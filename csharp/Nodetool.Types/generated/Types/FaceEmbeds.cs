using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types;

[MessagePackObject]
public class FaceEmbeds
{
    [Key(0)]
    public object type { get; set; } = "comfy.face_embeds";
    [Key(1)]
    public object data { get; set; } = null;
}
