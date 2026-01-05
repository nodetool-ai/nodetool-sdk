using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class SaveImageFile
{
    [Key(0)]
    public string filename { get; set; } = @"image.png";
    [Key(1)]
    public string folder { get; set; } = @".";
    [Key(2)]
    public Nodetool.Types.Core.ImageRef image { get; set; } = new Nodetool.Types.Core.ImageRef();
    [Key(3)]
    public bool overwrite { get; set; } = false;

    public Nodetool.Types.Core.ImageRef Process()
    {
        return default(Nodetool.Types.Core.ImageRef);
    }
}
