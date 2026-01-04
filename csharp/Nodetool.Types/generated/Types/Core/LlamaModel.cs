using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types.Core;

[MessagePackObject]
public class LlamaModel
{
    [Key(0)]
    public object details { get; set; }
    [Key(1)]
    public string digest { get; set; } = @"";
    [Key(2)]
    public string modified_at { get; set; } = @"";
    [Key(3)]
    public string name { get; set; } = @"";
    [Key(4)]
    public string repo_id { get; set; } = @"";
    [Key(5)]
    public int size { get; set; } = 0;
    [Key(6)]
    public object type { get; set; } = @"llama_model";
}
