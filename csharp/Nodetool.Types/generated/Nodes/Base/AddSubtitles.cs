using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class AddSubtitles
{
    [Key(0)]
    public Nodetool.Types.VideoRef video { get; set; } = new Nodetool.Types.VideoRef();
    [Key(1)]
    public object chunks { get; set; } = new List<object>();
    [Key(2)]
    public Nodetool.Types.FontRef font { get; set; } = new Nodetool.Types.FontRef();
    [Key(3)]
    public object align { get; set; } = "SubtitleTextAlignment.BOTTOM";
    [Key(4)]
    public int font_size { get; set; } = 24;
    [Key(5)]
    public Nodetool.Types.ColorRef font_color { get; set; } = new Nodetool.Types.ColorRef();

    public Nodetool.Types.VideoRef Process()
    {
        return default(Nodetool.Types.VideoRef);
    }
}
