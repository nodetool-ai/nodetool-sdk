using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class SaveDocumentFile
{
    [Key(0)]
    public Nodetool.Types.Core.DocumentRef document { get; set; } = new Nodetool.Types.Core.DocumentRef();
    [Key(1)]
    public string filename { get; set; } = @"";
    [Key(2)]
    public string folder { get; set; } = @"";

    public void Process()
    {
    }
}
