using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class AddSubtitles
{
    [Key(0)]
    public object align { get; set; } = @"bottom";
    [Key(1)]
    public object chunks { get; set; } = new();
    [Key(2)]
    public Nodetool.Types.Core.FontRef font { get; set; } = new Nodetool.Types.Core.FontRef();
    [Key(3)]
    public Nodetool.Types.Core.ColorRef font_color { get; set; } = new Nodetool.Types.Core.ColorRef();
    [Key(4)]
    public int font_size { get; set; } = 24;
    [Key(5)]
    public Nodetool.Types.Core.VideoRef video { get; set; } = new Nodetool.Types.Core.VideoRef();

    public Nodetool.Types.Core.VideoRef Process()
    {
        return default(Nodetool.Types.Core.VideoRef);
    }
}
