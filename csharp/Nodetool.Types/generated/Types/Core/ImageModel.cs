using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types.Core;

[MessagePackObject]
public class ImageModel
{
    [Key(0)]
    public string id { get; set; } = @"";
    [Key(1)]
    public string name { get; set; } = @"";
    [Key(2)]
    public object path { get; set; } = null;
    [Key(3)]
    public object provider { get; set; } = @"empty";
    [Key(4)]
    public List<string> supported_tasks { get; set; }
    [Key(5)]
    public object type { get; set; } = @"image_model";
}
