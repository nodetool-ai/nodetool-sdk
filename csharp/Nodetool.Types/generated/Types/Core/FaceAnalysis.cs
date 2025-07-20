using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types;

[MessagePackObject]
public class FaceAnalysis
{
    [Key(0)]
    public object type { get; set; } = "comfy.face_analysis";
    [Key(1)]
    public object data { get; set; } = null;
}
