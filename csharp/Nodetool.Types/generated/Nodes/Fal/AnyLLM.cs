using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Fal;

[MessagePackObject]
public class AnyLLM
{
    [Key(0)]
    public string prompt { get; set; } = "";
    [Key(1)]
    public string system_prompt { get; set; } = "";
    [Key(2)]
    public object model { get; set; } = "ModelEnum.GEMINI_FLASH";

    public string Process()
    {
        // Implementation would be generated based on node logic
        return default(string);
    }
}
