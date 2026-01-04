using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class HuggingFaceModelInput
{
    [Key(0)]
    public string description { get; set; } = @"";
    [Key(1)]
    public string name { get; set; } = @"";
    [Key(2)]
    public Nodetool.Types.Core.HuggingFaceModel value { get; set; } = new Nodetool.Types.Core.HuggingFaceModel();

    public Nodetool.Types.Core.HuggingFaceModel Process()
    {
        return default(Nodetool.Types.Core.HuggingFaceModel);
    }
}
