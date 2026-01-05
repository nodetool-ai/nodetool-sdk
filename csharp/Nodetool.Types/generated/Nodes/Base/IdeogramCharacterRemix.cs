using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class IdeogramCharacterRemix
{
    [Key(0)]
    public object additional_images { get; set; } = new();
    [Key(1)]
    public bool expand_prompt { get; set; } = true;
    [Key(2)]
    public Nodetool.Types.Core.ImageRef image { get; set; } = new Nodetool.Types.Core.ImageRef();
    [Key(3)]
    public object image_size { get; set; } = @"square_hd";
    [Key(4)]
    public string negative_prompt { get; set; } = @"";
    [Key(5)]
    public string prompt { get; set; } = @"";
    [Key(6)]
    public object reference_images { get; set; } = new();
    [Key(7)]
    public string reference_mask_urls { get; set; } = @"";
    [Key(8)]
    public object rendering_speed { get; set; } = @"BALANCED";
    [Key(9)]
    public double strength { get; set; } = 0.8;
    [Key(10)]
    public object style { get; set; } = @"AUTO";

    public Nodetool.Types.Core.ImageRef Process()
    {
        return default(Nodetool.Types.Core.ImageRef);
    }
}
