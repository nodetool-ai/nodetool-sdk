using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types.Core;

[MessagePackObject]
public class ASRModel
{
    [Key(0)]
    public string id { get; set; } = @"";
    [Key(1)]
    public string name { get; set; } = @"";
    [Key(2)]
    public object path { get; set; } = null;
    [Key(3)]
    public object provider { get; set; } = @"empty";
    [Key(4)]
    public object type { get; set; } = @"asr_model";
}
