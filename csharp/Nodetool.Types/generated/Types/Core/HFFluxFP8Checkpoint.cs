using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types.Core;

[MessagePackObject]
public class HFFluxFP8Checkpoint
{
    [Key(0)]
    public object allow_patterns { get; set; } = null;
    [Key(1)]
    public object ignore_patterns { get; set; } = null;
    [Key(2)]
    public object path { get; set; } = null;
    [Key(3)]
    public string repo_id { get; set; } = @"";
    [Key(4)]
    public object type { get; set; } = @"hf.flux_fp8_checkpoint";
    [Key(5)]
    public object variant { get; set; } = null;
}
