using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types.Core;

[MessagePackObject]
public class FaceAnalysis
{
    [Key(0)]
    public object data { get; set; } = null;
    [Key(1)]
    public object type { get; set; } = @"comfy.face_analysis";
}
