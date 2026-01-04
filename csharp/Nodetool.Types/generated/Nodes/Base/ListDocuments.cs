using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class ListDocuments
{
    [Key(0)]
    public string folder { get; set; } = @"~";
    [Key(1)]
    public string pattern { get; set; } = @"*";
    [Key(2)]
    public bool recursive { get; set; } = false;

    public Nodetool.Types.Core.DocumentRef Process()
    {
        return default(Nodetool.Types.Core.DocumentRef);
    }
}
