using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types.Core;

[MessagePackObject]
public class GLIGENFile
{
    [Key(0)]
    public object type { get; set; } = "comfy.gligen_file";
    [Key(1)]
    public string name { get; set; } = "";
}
