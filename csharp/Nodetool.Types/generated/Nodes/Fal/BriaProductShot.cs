using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Fal;

[MessagePackObject]
public class BriaProductShot
{
    [Key(0)]
    public Nodetool.Types.Core.ImageRef image { get; set; } = new Nodetool.Types.Core.ImageRef();
    [Key(1)]
    public string manual_placement_selection { get; set; } = @"bottom_center";
    [Key(2)]
    public bool optimize_description { get; set; } = true;
    [Key(3)]
    public string placement_type { get; set; } = @"manual_placement";
    [Key(4)]
    public Nodetool.Types.Core.ImageRef ref_image { get; set; } = new Nodetool.Types.Core.ImageRef();
    [Key(5)]
    public string scene_description { get; set; } = @"";

    public Nodetool.Types.Core.ImageRef Process()
    {
        return default(Nodetool.Types.Core.ImageRef);
    }
}
