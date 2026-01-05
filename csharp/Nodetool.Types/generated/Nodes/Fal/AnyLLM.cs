using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Fal;

[MessagePackObject]
public class AnyLLM
{
    [Key(0)]
    public object model { get; set; } = @"google/gemini-flash-1.5";
    [Key(1)]
    public string prompt { get; set; } = @"";
    [Key(2)]
    public string system_prompt { get; set; } = @"";

    public string Process()
    {
        return default(string);
    }
}
