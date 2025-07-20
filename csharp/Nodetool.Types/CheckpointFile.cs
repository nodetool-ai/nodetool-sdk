using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types;

[MessagePackObject]
public class CheckpointFile
{
    [Key(0)]
    public object type { get; set; } = "comfy.checkpoint_file";
    [Key(1)]
    public string name { get; set; } = "";
}
