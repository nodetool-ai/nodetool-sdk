using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Fal;

[MessagePackObject]
public class Recraft20B
{
    [Key(0)]
    public object colors { get; set; } = new();
    [Key(1)]
    public object image_size { get; set; } = @"square_hd";
    [Key(2)]
    public string prompt { get; set; } = @"";
    [Key(3)]
    public object style { get; set; } = @"realistic_image";
    [Key(4)]
    public string style_id { get; set; } = @"";

    public Nodetool.Types.Core.ImageRef Process()
    {
        return default(Nodetool.Types.Core.ImageRef);
    }
}
