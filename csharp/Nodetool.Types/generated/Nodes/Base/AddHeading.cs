using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class AddHeading
{
    [Key(0)]
    public Nodetool.Types.Core.DocumentRef document { get; set; } = new Nodetool.Types.Core.DocumentRef();
    [Key(1)]
    public int level { get; set; } = 1;
    [Key(2)]
    public string text { get; set; } = @"";

    public Nodetool.Types.Core.DocumentRef Process()
    {
        return default(Nodetool.Types.Core.DocumentRef);
    }
}
