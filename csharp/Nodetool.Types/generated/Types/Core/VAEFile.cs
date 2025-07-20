using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types;

[MessagePackObject]
public class VAEFile
{
    [Key(0)]
    public object type { get; set; } = "comfy.vae_file";
    [Key(1)]
    public string name { get; set; } = "";
}
