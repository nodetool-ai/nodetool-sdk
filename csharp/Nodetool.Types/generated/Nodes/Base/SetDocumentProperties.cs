using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class SetDocumentProperties
{
    [Key(0)]
    public string author { get; set; } = @"";
    [Key(1)]
    public Nodetool.Types.Core.DocumentRef document { get; set; } = new Nodetool.Types.Core.DocumentRef();
    [Key(2)]
    public string keywords { get; set; } = @"";
    [Key(3)]
    public string subject { get; set; } = @"";
    [Key(4)]
    public string title { get; set; } = @"";

    public Nodetool.Types.Core.DocumentRef Process()
    {
        return default(Nodetool.Types.Core.DocumentRef);
    }
}
