using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types.Core;

[MessagePackObject]
public class OpenAIModel
{
    [Key(0)]
    public object type { get; set; } = "openai_model";
    [Key(1)]
    public string id { get; set; } = "";
    [Key(2)]
    public string @object { get; set; } = "";
    [Key(3)]
    public int created { get; set; } = 0;
    [Key(4)]
    public string owned_by { get; set; } = "";
}
