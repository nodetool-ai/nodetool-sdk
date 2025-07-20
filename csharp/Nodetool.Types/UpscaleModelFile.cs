using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types;

[MessagePackObject]
public class UpscaleModelFile
{
    [Key(0)]
    public object type { get; set; } = "comfy.upscale_model_file";
    [Key(1)]
    public string name { get; set; } = "";
}
