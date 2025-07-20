using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types.Core;

[MessagePackObject]
public class InstantIDFile
{
    [Key(0)]
    public object type { get; set; } = "comfy.instant_id_file";
    [Key(1)]
    public string name { get; set; } = "";
}
