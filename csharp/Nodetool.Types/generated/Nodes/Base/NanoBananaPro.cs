using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class NanoBananaPro
{
    [Key(0)]
    public object aspect_ratio { get; set; } = @"1:1";
    [Key(1)]
    public object image_input { get; set; } = new();
    [Key(2)]
    public string prompt { get; set; } = @"";
    [Key(3)]
    public object resolution { get; set; } = @"2K";

    public Nodetool.Types.Core.ImageRef Process()
    {
        return default(Nodetool.Types.Core.ImageRef);
    }
}
