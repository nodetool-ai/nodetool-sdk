using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types.Core;

[MessagePackObject]
public class HFT5
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
    public object type { get; set; } = @"hf.t5";
    [Key(5)]
    public object variant { get; set; } = null;
}
