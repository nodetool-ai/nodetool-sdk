using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types;

[MessagePackObject]
public class LlamaModel
{
    [Key(0)]
    public object type { get; set; } = "llama_model";
    [Key(1)]
    public string name { get; set; } = "";
    [Key(2)]
    public string repo_id { get; set; } = "";
    [Key(3)]
    public string modified_at { get; set; } = "";
    [Key(4)]
    public int size { get; set; } = 0;
    [Key(5)]
    public string digest { get; set; } = "";
    [Key(6)]
    public object details { get; set; }
}
