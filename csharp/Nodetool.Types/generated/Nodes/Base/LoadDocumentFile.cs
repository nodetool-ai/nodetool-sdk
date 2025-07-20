using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class LoadDocumentFile
{
    [Key(0)]
    public Nodetool.Types.FilePath path { get; set; } = new Nodetool.Types.FilePath();

    public Nodetool.Types.DocumentRef Process()
    {
        return default(Nodetool.Types.DocumentRef);
    }
}
