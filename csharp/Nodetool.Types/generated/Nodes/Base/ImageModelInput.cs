using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class ImageModelInput
{
    [Key(0)]
    public string description { get; set; } = @"";
    [Key(1)]
    public string name { get; set; } = @"";
    [Key(2)]
    public Nodetool.Types.Core.ImageModel value { get; set; } = new Nodetool.Types.Core.ImageModel();

    public Nodetool.Types.Core.ImageModel Process()
    {
        return default(Nodetool.Types.Core.ImageModel);
    }
}
