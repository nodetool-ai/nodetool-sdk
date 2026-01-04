using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class AddParagraph
{
    [Key(0)]
    public object alignment { get; set; } = @"LEFT";
    [Key(1)]
    public bool bold { get; set; } = false;
    [Key(2)]
    public Nodetool.Types.Core.DocumentRef document { get; set; } = new Nodetool.Types.Core.DocumentRef();
    [Key(3)]
    public int font_size { get; set; } = 12;
    [Key(4)]
    public bool italic { get; set; } = false;
    [Key(5)]
    public string text { get; set; } = @"";

    public Nodetool.Types.Core.DocumentRef Process()
    {
        return default(Nodetool.Types.Core.DocumentRef);
    }
}
