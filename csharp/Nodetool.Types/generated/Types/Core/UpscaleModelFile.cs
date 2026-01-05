using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types.Core;

[MessagePackObject]
public class UpscaleModelFile
{
    [Key(0)]
    public string name { get; set; } = @"";
    [Key(1)]
    public object type { get; set; } = @"comfy.upscale_model_file";
}
