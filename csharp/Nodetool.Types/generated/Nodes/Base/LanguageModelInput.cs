using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class LanguageModelInput
{
    [Key(0)]
    public string description { get; set; } = @"";
    [Key(1)]
    public string name { get; set; } = @"";
    [Key(2)]
    public Nodetool.Types.Core.LanguageModel value { get; set; } = new Nodetool.Types.Core.LanguageModel();

    public Nodetool.Types.Core.LanguageModel Process()
    {
        return default(Nodetool.Types.Core.LanguageModel);
    }
}
