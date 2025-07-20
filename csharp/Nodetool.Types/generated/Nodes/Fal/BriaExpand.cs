using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Fal;

[MessagePackObject]
public class BriaExpand
{
    [Key(0)]
    public Nodetool.Types.ImageRef image { get; set; } = new Nodetool.Types.ImageRef();
    [Key(1)]
    public int canvas_width { get; set; } = 1200;
    [Key(2)]
    public int canvas_height { get; set; } = 674;
    [Key(3)]
    public int original_image_width { get; set; } = 610;
    [Key(4)]
    public int original_image_height { get; set; } = 855;
    [Key(5)]
    public int original_image_x { get; set; } = 301;
    [Key(6)]
    public int original_image_y { get; set; } = -66;
    [Key(7)]
    public string prompt { get; set; } = "";
    [Key(8)]
    public string negative_prompt { get; set; } = "";
    [Key(9)]
    public int num_images { get; set; } = 1;
    [Key(10)]
    public int seed { get; set; } = -1;

    public Nodetool.Types.ImageRef Process()
    {
        return default(Nodetool.Types.ImageRef);
    }
}
