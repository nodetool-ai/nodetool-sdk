using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types;

[MessagePackObject]
public class REMBGSession
{
    [Key(0)]
    public object type { get; set; } = "comfy.rembg_session";
    [Key(1)]
    public object data { get; set; } = null;
}
