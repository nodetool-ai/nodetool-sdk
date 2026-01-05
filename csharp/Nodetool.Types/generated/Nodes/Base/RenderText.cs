using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class RenderText
{
    [Key(0)]
    public object align { get; set; } = @"left";
    [Key(1)]
    public Nodetool.Types.Core.ColorRef color { get; set; } = new Nodetool.Types.Core.ColorRef();
    [Key(2)]
    public Nodetool.Types.Core.FontRef font { get; set; } = new Nodetool.Types.Core.FontRef();
    [Key(3)]
    public Nodetool.Types.Core.ImageRef image { get; set; } = new Nodetool.Types.Core.ImageRef();
    [Key(4)]
    public int size { get; set; } = 12;
    [Key(5)]
    public string text { get; set; } = @"";
    [Key(6)]
    public int x { get; set; } = 0;
    [Key(7)]
    public int y { get; set; } = 0;

    public Nodetool.Types.Core.ImageRef Process()
    {
        return default(Nodetool.Types.Core.ImageRef);
    }
}
