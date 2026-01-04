using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class NanoBananaEdit
{
    [Key(0)]
    public object image_input { get; set; } = new();
    [Key(1)]
    public object image_size { get; set; } = @"1:1";
    [Key(2)]
    public string prompt { get; set; } = @"";

    public Nodetool.Types.Core.ImageRef Process()
    {
        return default(Nodetool.Types.Core.ImageRef);
    }
}
