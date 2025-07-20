using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Nodetool_fal;

[MessagePackObject]
public class Recraft20B
{
    [Key(0)]
    public string prompt { get; set; } = "";
    [Key(1)]
    public object image_size { get; set; } = "ImageSizePreset.SQUARE_HD";
    [Key(2)]
    public object style { get; set; } = "StylePreset.REALISTIC_IMAGE";
    [Key(3)]
    public object colors { get; set; } = new List<object>();
    [Key(4)]
    public string style_id { get; set; } = "";

    public Nodetool.Types.ImageRef Process()
    {
        // Implementation would be generated based on node logic
        return default(Nodetool.Types.ImageRef);
    }
}
